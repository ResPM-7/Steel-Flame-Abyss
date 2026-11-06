using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

//풀에서 사용하는 씬 조합
[System.Flags]
public enum PoolScope
{
    None = 0,
    Title = 1,
    Map = 2,
    Battle = 4
}

//시트에서 관리하는 풀 공통 데이터
[System.Serializable]
public class PoolDefinition
{
    public string poolName;
    public PoolScope scope;
    public string parentKey;
    public int poolSize;
    public AssetReferenceGameObject addressablePrefab;
}

//씬 참조와 분리된 공통 풀 목록
[CreateAssetMenu(menuName = "Game Data/Pool Database", fileName = "PoolDatabase")]
public class PoolDatabase : ScriptableObject
{
    [SerializeField] private List<PoolDefinition> objectPools = new List<PoolDefinition>();
    [SerializeField] private List<PoolDefinition> canvasPools = new List<PoolDefinition>();
    public IReadOnlyList<PoolDefinition> ObjectPools => objectPools;
    public IReadOnlyList<PoolDefinition> CanvasPools => canvasPools;

#if UNITY_EDITOR
    //검증 완료된 시트 데이터 전체 교체
    public void ReplaceFromSheet(List<PoolDefinition> objects, List<PoolDefinition> canvases)
    {
        objectPools = objects;
        canvasPools = canvases;
    }
#endif
}
