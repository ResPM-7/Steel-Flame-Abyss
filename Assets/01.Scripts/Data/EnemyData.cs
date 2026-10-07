using System.Collections.Generic;
using UnityEngine;

//적의 체력 범위 보상과 행동 패턴을 보관
public class EnemyData : GameDataEntry
{
    [SerializeField] private EnemyTier tier;
    [Min(1)] [SerializeField] private int minHealth = 1;
    [Min(1)] [SerializeField] private int maxHealth = 1;
    [Min(0)] [SerializeField] private int minGold;
    [Min(0)] [SerializeField] private int maxGold;
    [SerializeField] private List<EnemyActionSpec> actions = new();
    [SerializeField] private Sprite artwork;

    public EnemyTier Tier => tier;
    public int MinHealth => minHealth;
    public int MaxHealth => maxHealth;
    public int MinGold => minGold;
    public int MaxGold => maxGold;
    public IReadOnlyList<EnemyActionSpec> Actions => actions;
    public Sprite Artwork => artwork;

#if UNITY_EDITOR
    //검증이 끝난 적 시트 한 행을 이 서브에셋에 반영
    public void EditorApply(EnemyTier newTier, int newMinHealth, int newMaxHealth, int newMinGold,
        int newMaxGold, List<EnemyActionSpec> newActions)
    {
        tier = newTier;
        minHealth = newMinHealth;
        maxHealth = newMaxHealth;
        minGold = newMinGold;
        maxGold = newMaxGold;
        actions = newActions;
    }
#endif
}
