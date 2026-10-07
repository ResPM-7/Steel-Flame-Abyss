using System.Collections.Generic;
using UnityEngine;

//유물의 발동 시점과 조합 가능한 효과 목록을 보관
//MVP에서 시트 주소가 비어 있을 때 생성 생략
public class RelicData : GameDataEntry
{
    [SerializeField] private DataRarity rarity;
    [TextArea(2, 5)] [SerializeField] private string description;
    [SerializeField] private RelicTrigger trigger;
    [SerializeField] private List<EffectSpec> effects = new();
    [SerializeField] private Sprite icon;

    public DataRarity Rarity => rarity;
    public string Description => description;
    public RelicTrigger Trigger => trigger;
    public IReadOnlyList<EffectSpec> Effects => effects;
    public Sprite Icon => icon;

#if UNITY_EDITOR
    //검증이 끝난 유물 시트 한 행을 이 서브에셋에 반영
    public void EditorApply(DataRarity newRarity, string newDescription, RelicTrigger newTrigger,
        List<EffectSpec> newEffects)
    {
        rarity = newRarity;
        description = newDescription;
        trigger = newTrigger;
        effects = newEffects;
    }
#endif
}
