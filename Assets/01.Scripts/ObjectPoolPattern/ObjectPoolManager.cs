using System.Collections.Generic;
using UnityEngine;

//일반 및 Canvas 오브젝트 풀 관리
public class ObjectPoolManager : Singleton<ObjectPoolManager>
{
    [System.Serializable]
    public struct CanvasPoolItem
    {
        public string poolName;
        public GameObject prefab;
        public Transform targetCanvas;
        public int poolSize;
    }


    [System.Serializable]
    public struct ObjectPoolItem
    {
        public string poolName;
        public GameObject prefab;
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



    //일반 풀 그룹과 Canvas 직속 오브젝트 풀 생성
    void Start()
    {
        //일반 오브젝트 풀 생성
        foreach (var item in objList)
        {
            //이름이나 프리팩이 없는 설정 제외
            if (string.IsNullOrEmpty(item.poolName) || item.prefab == null) continue;

            prefabDict[item.poolName] = item.prefab;
            pools[item.poolName] = new Queue<GameObject>();

            GameObject parentPool = new GameObject($"{item.poolName}_Pool");
            parentPool.transform.SetParent(this.transform);

            SetupPool(item.poolName, item.prefab, parentPool.transform, item.poolSize);
        }

        //Canvas 오브젝트 풀 생성
        foreach (var item in canvasPools)
        {
            if (string.IsNullOrEmpty(item.poolName) || item.prefab == null) continue;
            if (item.targetCanvas == null)
            {
                Debug.LogError($"[오브젝트 풀] {item.poolName}의 Target Canvas를 연결해 주세요", this);
                continue;
            }

            prefabDict[item.poolName] = item.prefab;
            pools[item.poolName] = new Queue<GameObject>();

            SetupPool(item.poolName, item.prefab, item.targetCanvas, item.poolSize);
        }
    }

    //지정한 개수만큼 비활성 오브젝트 생성
    private void SetupPool(string poolName, GameObject prefab, Transform parent, int size)
    {
        poolParents[poolName] = parent;

        for (int i = 0; i < size; i++)
        {
            GameObject go = Instantiate(prefab, parent);
            go.name = poolName;
            go.SetActive(false);
            availableObjects.Add(go);
            pools[poolName].Enqueue(go);
        }
    }

    //풀에서 오브젝트를 가져오거나 부족할 때 추가 생성
    public GameObject GetObject(string poolName)
    {
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
        pendingReturns.Clear();
        pendingObjects.Clear();
        availableObjects.Clear();
        pools.Clear();
        poolParents.Clear();
        prefabDict.Clear();
        base.OnDestroy();
    }
}
