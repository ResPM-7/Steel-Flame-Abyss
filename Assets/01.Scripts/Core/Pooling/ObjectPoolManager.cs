using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum SceneScope
{
    None,
    Title,
    Map,
    Battle
}


//일반 및 Canvas 오브젝트 풀 관리
public class ObjectPoolManager : Singleton<ObjectPoolManager>
{
    [System.Serializable]
    public struct ParentBinding
    {
        public string key;
        public Transform parent;
    }
    //현재 이곳의 씬
    [SerializeField] public SceneScope scenescope = SceneScope.None;
    [SerializeField] private PoolDatabase database;
    //시트 순서와 독립적인 씬 부모 연결
    [SerializeField] private List<ParentBinding> parentBindings = new List<ParentBinding>();
    private readonly Dictionary<string, Transform> parentsByKey = new Dictionary<string, Transform>(System.StringComparer.Ordinal);
    private readonly HashSet<string> reportedMissingPools = new HashSet<string>();

    private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, Transform> poolParents = new Dictionary<string, Transform>();
    private Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();
    private readonly HashSet<GameObject> availableObjects = new HashSet<GameObject>();
    private readonly HashSet<GameObject> pendingObjects = new HashSet<GameObject>();
    private readonly Queue<KeyValuePair<string, GameObject>> pendingReturns = new Queue<KeyValuePair<string, GameObject>>();
    private bool shuttingDown;
    private bool initialized;
    private readonly Dictionary<string, PoolDefinition> addressablePools = new Dictionary<string, PoolDefinition>();
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
        if (database == null || scenescope == SceneScope.None || !System.Enum.IsDefined(typeof(SceneScope), scenescope))
        {
            GameLogger.LogError("[오브젝트 풀] Pool Database와 현재 Scene Scope를 지정해 주세요", this);
            return;
        }
        foreach (var binding in parentBindings)
        {
            if (string.IsNullOrWhiteSpace(binding.key)) continue;
            if (!parentsByKey.TryAdd(binding.key.Trim(), binding.parent))
                GameLogger.LogError($"[오브젝트 풀] 중복 부모 키: {binding.key}", this);
        }
        //일반 오브젝트 풀 생성
        foreach (var item in database.ObjectPools)
        {
            if (!MatchesScope(item)) continue;
            Transform parent = string.IsNullOrWhiteSpace(item.parentKey) ? transform : ResolveParent(item);
            if (!RegisterPool(item, parent)) continue;

            GameObject parentPool = new GameObject($"{item.poolName}_Pool");
            parentPool.transform.SetParent(parent, false);
            poolParents[item.poolName] = parentPool.transform;
        }

        //Canvas 오브젝트 풀 생성
        foreach (var item in database.CanvasPools)
        {
            if (!MatchesScope(item)) continue;
            RegisterPool(item, ResolveParent(item));
        }
    }

    //현재 씬에 해당하는 풀만 선택
    private bool MatchesScope(PoolDefinition item)
    {
        if (item == null) return false;
        PoolScope current = scenescope == SceneScope.Title ? PoolScope.Title
            : scenescope == SceneScope.Map ? PoolScope.Map : PoolScope.Battle;
        return (item.scope & current) != 0;
    }

    //부모 키로 현재 씬의 생성 위치 조회
    private Transform ResolveParent(PoolDefinition item)
    {
        if (!string.IsNullOrWhiteSpace(item.parentKey) &&
            parentsByKey.TryGetValue(item.parentKey.Trim(), out Transform parent) && parent != null)
            return parent;
        GameLogger.LogError($"[오브젝트 풀] {item.poolName}의 부모 키 '{item.parentKey}'를 Parent Bindings에 연결해 주세요", this);
        return null;
    }

    //풀 이름과 에셋 참조 및 부모 검증 후 공통 등록
    private bool RegisterPool(PoolDefinition item, Transform parent)
    {
        if (string.IsNullOrWhiteSpace(item.poolName)) return false;
        if (parent == null) return false;
        if (item.addressablePrefab == null || !item.addressablePrefab.RuntimeKeyIsValid())
        {
            GameLogger.LogError($"[오브젝트 풀] {item.poolName}의 Addressable Prefab과 부모를 연결해 주세요", this);
            return false;
        }
        if (pools.ContainsKey(item.poolName))
        {
            GameLogger.LogError($"[오브젝트 풀] 중복 이름 {item.poolName}", this);
            return false;
        }
        pools[item.poolName] = new Queue<GameObject>();
        poolParents[item.poolName] = parent;
        addressablePools[item.poolName] = item;
        return true;
    }

    //현재 씬에 없는 풀 요청을 이름별 한 번 보고
    private bool IsRegistered(string poolName)
    {
        if (string.IsNullOrWhiteSpace(poolName)) return false;
        if (pools.ContainsKey(poolName)) return true;
        if (reportedMissingPools.Add(poolName))
            GameLogger.LogError($"[오브젝트 풀] {poolName} 미등록: {scenescope} 범위와 데이터 및 부모 연결 확인", this);
        return false;
    }

    //풀 최초 로드와 동시 요청의 로딩 작업 공유
    public Task<bool> EnsurePoolReadyAsync(string poolName)
    {
        InitializePools();
        if (shuttingDown || !IsRegistered(poolName)) return Task.FromResult(false);
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
    private async Task<bool> LoadPoolAsync(PoolDefinition item)
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
                GameLogger.LogError($"[오브젝트 풀] {item.poolName} 로드 실패: {error.Message}", this);
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
        if (shuttingDown || !IsRegistered(poolName))
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
        parentsByKey.Clear();
        reportedMissingPools.Clear();
        prefabDict.Clear();
        base.OnDestroy();
    }

    //사용 중 복제본 제거 완료 후 원본 로드 핸들 해제
    private static async void ReleaseAfterInstancesDestroyed(GameObject[] instances, List<AsyncOperationHandle<GameObject>> handles)
    {
        while (Application.isPlaying && System.Array.Exists(instances, item => item != null))
            await Task.Delay(16);
        foreach (var handle in handles)
            if (handle.IsValid()) Addressables.Release(handle);
    }
}
