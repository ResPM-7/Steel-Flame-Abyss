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



    //등록된 일반 및 Canvas 오브젝트 풀 생성
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

            prefabDict[item.poolName] = item.prefab;
            pools[item.poolName] = new Queue<GameObject>();

            GameObject parentPool = new GameObject($"{item.poolName}_Pool");
            //UI 크기 변형 방지를 위한 로컬 Transform 유지
            parentPool.transform.SetParent(item.targetCanvas, false);

            SetupPool(item.poolName, item.prefab, parentPool.transform, item.poolSize);
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
            pools[poolName].Enqueue(go);
        }
    }

    //풀에서 오브젝트를 가져오거나 부족할 때 추가 생성
    public GameObject GetObject(string poolName)
    {
        if (!pools.ContainsKey(poolName))
        {
            return null;
        }

        if (pools[poolName].Count > 0)
        {
            GameObject go = pools[poolName].Dequeue();
            go.SetActive(true);
            return go;
        }
        else
        {
            GameObject prefab = GetPrefabFromList(poolName);

            if (prefab == null) return null;

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

    //사용한 오브젝트를 비활성화해 풀에 반환
    public void ReturnObject(string poolName, GameObject go)
    {
        if (!pools.ContainsKey(poolName))
        {
            Destroy(go);
            return;
        }
        go.SetActive(false);
        go.transform.SetParent(poolParents[poolName]);
        pools[poolName].Enqueue(go);
    }

}
