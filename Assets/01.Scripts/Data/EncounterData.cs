using System.Collections.Generic;
using UnityEngine;

//전투 편성의 적 ID와 배치 순서 보관
public class EncounterData : GameDataEntry
{
    [Tooltip("EnemyPoint1~3 배치 순서, 0은 빈 자리")]
    [SerializeField] private int[] enemyIds = new int[3];

    public IReadOnlyList<int> EnemyIds => enemyIds;

#if UNITY_EDITOR
    //검증된 시트의 적 배치 정보 받음
    public void EditorApply(int[] newEnemyIds)
    {
        enemyIds = (int[])newEnemyIds.Clone();
    }
#endif
}
