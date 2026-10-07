using System.Collections.Generic;
using UnityEngine;

//카드 한 장의 비용 효과 강화 효과와 표시 리소스를 보관
public class CardData : GameDataEntry
{
    [SerializeField] private CharacterClass owner;
    [SerializeField] private CardType cardType;
    [SerializeField] private DataRarity rarity;
    [Min(0)] [SerializeField] private int cost;
    [TextArea(2, 5)] [SerializeField] private string description;
    [TextArea(2, 5)] [SerializeField] private string upgradedDescription;
    [SerializeField] private List<EffectSpec> effects = new();
    [SerializeField] private List<EffectSpec> upgradedEffects = new();
    [SerializeField] private List<string> keywords = new();
    [SerializeField] private Sprite artwork;

    public CharacterClass Owner => owner;
    public CardType CardType => cardType;
    public DataRarity Rarity => rarity;
    public int Cost => cost;
    public string Description => description;
    public string UpgradedDescription => upgradedDescription;
    public IReadOnlyList<EffectSpec> Effects => effects;
    public IReadOnlyList<EffectSpec> UpgradedEffects => upgradedEffects;
    public IReadOnlyList<string> Keywords => keywords;
    public Sprite Artwork => artwork;

#if UNITY_EDITOR
    //검증이 끝난 카드 시트 한 행을 이 서브에셋에 반영
    public void EditorApply(CharacterClass newOwner, CardType newCardType, DataRarity newRarity, int newCost,
        string newDescription, string newUpgradedDescription, List<EffectSpec> newEffects,
        List<EffectSpec> newUpgradedEffects, List<string> newKeywords)
    {
        owner = newOwner;
        cardType = newCardType;
        rarity = newRarity;
        cost = newCost;
        description = newDescription;
        upgradedDescription = newUpgradedDescription;
        effects = newEffects;
        upgradedEffects = newUpgradedEffects;
        keywords = newKeywords;
    }
#endif
}
