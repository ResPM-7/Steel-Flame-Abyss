using UnityEngine;

//등급별 추첨 가중치 보관, 합계 기준으로 확률 환산
[System.Serializable]
public class RarityWeights
{
    [Min(0f)] [SerializeField] private float starter = 0f;
    [Min(0f)] [SerializeField] private float common = 50f;
    [Min(0f)] [SerializeField] private float uncommon = 20f;
    [Min(0f)] [SerializeField] private float rare = 9f;
    [Min(0f)] [SerializeField] private float special = 1f;

    //잘못된 실수 값과 음수 가중치 제외
    public float GetWeight(DataRarity rarity)
    {
        float value = rarity switch
        {
            DataRarity.Starter => starter,
            DataRarity.Common => common,
            DataRarity.Uncommon => uncommon,
            DataRarity.Rare => rare,
            DataRarity.Special => special,
            _ => 0f
        };
        return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
    }
}
