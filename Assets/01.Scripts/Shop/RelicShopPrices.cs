using System;
using UnityEngine;

//등급별 유물 가격 설정
[Serializable]
public class RelicShopPrices
{
    [Min(1)] [SerializeField] private int starter = 100;
    [Min(1)] [SerializeField] private int common = 100;
    [Min(1)] [SerializeField] private int uncommon = 150;
    [Min(1)] [SerializeField] private int rare = 250;
    [Min(1)] [SerializeField] private int special = 350;

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
