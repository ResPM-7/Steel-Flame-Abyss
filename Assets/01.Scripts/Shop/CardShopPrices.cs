using System;
using UnityEngine;

//인스펙터에서 접고 펼치는 카드 등급별 가격
[Serializable]
public class CardShopPrices
{
    [Min(1)] [SerializeField] private int starter = 40;
    [Min(1)] [SerializeField] private int common = 50;
    [Min(1)] [SerializeField] private int uncommon = 80;
    [Min(1)] [SerializeField] private int rare = 120;
    [Min(1)] [SerializeField] private int special = 180;

    //상품 생성 시 등급별 가격 조회
    public int GetPrice(DataRarity rarity) => Mathf.Max(1, rarity switch
    {
        DataRarity.Starter => starter,
        DataRarity.Common => common,
        DataRarity.Uncommon => uncommon,
        DataRarity.Rare => rare,
        _ => special
    });
}
