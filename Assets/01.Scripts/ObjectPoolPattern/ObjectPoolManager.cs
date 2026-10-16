using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

//일반 및 Canvas 오브젝트 풀 관리
public class ObjectPoolManager : Singleton<ObjectPoolManager>
{
    [System.Serializable]
    public struct CanvasPoolItem
    {
        public string poolName;
        public Transform targetCanvas;
        public int poolSize;
        public AssetReferenceGameObject addressablePrefab;
    }


    [System.Serializable]
    public struct ObjectPoolItem
    {
        public string poolName;
        public AssetReferenceGameObject addressablePrefab;
        public int poolSize;
    }

    //일반 오브젝트 풀 설정
    [SerializeField] public List<ObjectPoolItem> objList = new List<ObjectPoolItem>();
    //Canvas 오브젝트 풀 설정
    [SerializeField] public List<CanvasPoolItem> canvasPools = new List<CanvasPoolItem>();

    private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, Transform> poolParents = new Dictionary<string, Transform>();
    private Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();
    private readonly HashSet<GameObject> availableObjects = new HashSet<GameObject>();
    private readonly HashSet<GameObject> pendingObjects = new HashSet<GameObject>();
    private readonly Queue<KeyValuePair<string, GameObject>> pendingReturns = new Queue<KeyValuePair<string, GameObject>>();
    private bool shuttingDown;
    private bool initialized;
    private readonly Dictionary<string, ObjectPoolItem> addressablePools = new Dictionary<string, ObjectPoolItem>();
    private readonly Dictionary<string, Task<bool>> loadingTasks = new Dictionary<string, Task<bool>>();
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> loadHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();
    private readonly List<GameObject> addressableInstances = new List<GameObject>();



    //일반 풀 그룹과 Canvas 직속 오브젝트 풀 생성
    void Start()
    {
        InitializePools();
    }

    //일반 풀과 Canvas 풀의 어드레서블 지연 등록
    private void InitializePools()
    {
        if (initialized || shuttingDown) return;
        initialized = true;
        //일반 오브젝트 풀 생성
        foreach (var item in objList)
        {
            if (!RegisterPool(item, transform)) continue;

            GameObject parentPool = new GameObject($"{item.poolName}_Pool");
            parentPool.transform.SetParent(transform, false);
            poolParents[item.poolName] = parentPool.transform;
        }

        //Canvas 오브젝트 풀 생성
        foreach (var item in canvasPools)
        {
            RegisterPool(new ObjectPoolItem
            {
                poolName = item.poolName,
                addressablePrefab = item.addressablePrefab,
                poolSize = item.poolSize
            }, item.targetCanvas);
        }
    }

    //풀 이름과 에셋 참조 및 부모 검증 후 공통 등록
    private bool RegisterPool(ObjectPoolItem item, Transform parent)
    {
        if (string.IsNullOrWhiteSpace(item.poolName)) return false;
        if (item.addressablePrefab == null || !item.addressablePrefab.RuntimeKeyIsValid() || parent == null)
        {
            Debug.LogError($"[오브젝트 풀] {item.poolName}의 Addressable Prefab과 부모를 연결해 주세요", this);
            return false;
        }
        if (pools.ContainsKey(item.poolName))
        {
            Debug.LogError($"[오브젝트 풀] 중복 이름 {item.poolName}", this);
            return false;
        }
        pools[item.poolName] = new Queue<GameObject>();
        poolParents[item.poolName] = parent;
        addressablePools[item.poolName] = item;
        return true;
    }

    //풀 최초 로드와 동시 요청의 로딩 작업 공유
    public Task<bool> EnsurePoolReadyAsync(string poolName)
    {
        InitializePools();
        if (shuttingDown || !pools.ContainsKey(poolName)) return Task.FromResult(false);
        if (prefabDict.ContainsKey(poolName)) return Task.FromResult(true);
        if (loadingTasks.TryGetValue(poolName, out var task))
        {
            if (!task.IsCompleted || task.Result) return task;
            loadingTasks.Remove(poolName);
        }
        if (!addressablePools.TryGetValue(poolName, out var item)) return Task.FromResult(false);
        task = LoadPoolAsync(item);
        loadingTasks[poolName] = task;
        return task;
    }

    //프리팹 로드 성공 시 풀 준비와 실패 핸들 해제
    private async Task<bool> LoadPoolAsync(ObjectPoolItem item)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(item.addressablePrefab.RuntimeKey);
        loadHandles[item.poolName] = handle;
        try
        {
            await handle.Task;
            if (shuttingDown || this == null) return false;
            if (handle.Status != AsyncOperationStatus.Succeeded || poolParents[item.poolName] == null)
                throw new System.InvalidOperationException($"풀 로드 실패 {item.poolName}");
            prefabDict[item.poolName] = handle.Result;
            SetupPool(item.poolName, handle.Result, poolParents[item.poolName], item.poolSize);
            return true;
        }
        catch (System.Exception error)
        {
            if (!shuttingDown && this != null)
            {
                Debug.LogError($"[오브젝트 풀] {item.poolName} 로드 실패: {error.Message}", this);
                loadHandles.Remove(item.poolName);
                if (handle.IsValid()) Addressables.Release(handle);
            }
            return false;
        }
    }

    //지정한 개수만큼 비활성 오브젝트 생성
    private void SetupPool(string poolName, GameObject prefab, Transform parent, int size)
    {
        poolParents[poolName] = parent;

        for (int i = 0; i < size; i++)
        {
            GameObject go = Instantiate(prefab, parent);
            if (addressablePools.ContainsKey(poolName)) addressableInstances.Add(go);
            go.name = poolName;
            go.SetActive(false);
            availableObjects.Add(go);
            pools[poolName].Enqueue(go);
        }
    }

    //풀에서 오브젝트를 가져오거나 부족할 때 추가 생성
    public GameObject GetObject(string poolName)
    {
        InitializePools();
        if (shuttingDown || !pools.ContainsKey(poolName))
        {
            return null;
        }

        while (pools[poolName].Count > 0)
        {
            GameObject go = pools[poolName].Dequeue();
            availableObjects.Remove(go);
            if (go == null) continue;
            go.SetActive(true);
            return go;
        }
        {
            GameObject prefab = GetPrefabFromList(poolName);

            if (prefab == null || poolParents[poolName] == null) return null;

            GameObject go = Instantiate(prefab, poolParents[poolName]);
            if (addressablePools.ContainsKey(poolName)) addressableInstances.Add(go);
            go.name = poolName;
            go.SetActive(true);
            return go;
        }
    }

    //풀 이름으로 등록된 프리팩 조회
    private GameObject GetPrefabFromList(string poolName)
    {
        if (prefabDict.TryGetValue(poolName, out GameObject prefab))
        {
            return prefab;
        }
        return null;
    }

    //중복 반환 차단과 비활성 오브젝트의 풀 복귀
    public void ReturnObject(string poolName, GameObject go)
    {
        if (shuttingDown || go == null || availableObjects.Contains(go) || pendingObjects.Contains(go)) return;
        if (!pools.ContainsKey(poolName) || poolParents[poolName] == null)
        {
            Destroy(go);
            return;
        }
        availableObjects.Add(go);
        go.SetActive(false);
        if (go == null)
        {
            availableObjects.Remove(go);
            return;
        }
        go.transform.SetParent(poolParents[poolName], false);
        pools[poolName].Enqueue(go);
    }

    //활성화 상태 변경 중 부모 이동 없는 반환 예약
    public void RequestReturn(string poolName, GameObject go)
    {
        if (shuttingDown || go == null || availableObjects.Contains(go) || !pendingObjects.Add(go)) return;
        pendingReturns.Enqueue(new KeyValuePair<string, GameObject>(poolName, go));
    }

    //비활성화 콜백 종료 후 예약 카드 반환
    private void LateUpdate()
    {
        int count = pendingReturns.Count;
        for (int i = 0; i < count; i++)
        {
            var request = pendingReturns.Dequeue();
            pendingObjects.Remove(request.Value);
            if (request.Value != null) ReturnObject(request.Key, request.Value);
        }
    }

    //씬 종료 시 부모 이동 없이 풀 참조 해제
    protected override void OnDestroy()
    {
        shuttingDown = true;
        foreach (var instance in addressableInstances)
            if (instance != null) Destroy(instance);
        ReleaseAfterInstancesDestroyed(addressableInstances.ToArray(), new List<AsyncOperationHandle<GameObject>>(loadHandles.Values));
        addressableInstances.Clear();
        loadHandles.Clear();
        loadingTasks.Clear();
        addressablePools.Clear();
        pendingReturns.Clear();
        pendingObjects.Clear();
        availableObjects.Clear();
        pools.Clear();
        poolParents.Clear();
        prefabDict.Clear();
        base.OnDestroy();
    }

    //사용 중 복제본 제거 완료 후 원본 로드 핸들 해제
    private static async void ReleaseAfterInstancesDestroyed(GameObject[] instances, List<AsyncOperationHandle<GameObject>> handles)
    {
        while (Application.isPlaying && System.Array.Exists(instances, item => item != null))
            await Task.Yield();
        foreach (var handle in handles)
            if (handle.IsValid()) Addressables.Release(handle);
    }
}
