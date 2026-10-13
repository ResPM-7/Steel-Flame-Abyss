using UnityEngine;

//모든 카드가 공유하는 희귀도별 테두리 색상 보관
[CreateAssetMenu(fileName = "CardRarityColors", menuName = "강철과 불꽃과 심연/카드 등급 색상")]
public class CardRarityColors : ScriptableObject
{
    [SerializeField] private Color starterColor;
    [SerializeField] private Color commonColor;
    [SerializeField] private Color uncommonColor;
    [SerializeField] private Color rareColor;
    [SerializeField] private Color specialColor;

    //카드 희귀도에 대응하는 공통 색상 반환
    public Color GetColor(DataRarity rarity)
    {
        switch (rarity)
        {
            case DataRarity.Starter: return starterColor;
            case DataRarity.Common: return commonColor;
            case DataRarity.Uncommon: return uncommonColor;
            case DataRarity.Rare: return rareColor;
            case DataRarity.Special: return specialColor;
            default: return commonColor;
        }
    }
}
