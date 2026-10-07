using System.Collections.Generic;
using UnityEngine;

namespace SteelFlameAbyss.Data
{
    /// <summary>유물의 발동 시점과 조합 가능한 효과 목록을 보관합니다.</summary>
    /// <remarks>MVP에서는 시트 주소가 비어 있으면 생성되지 않으며, 이후 유물 기능 추가에 대비한 형식만 유지합니다.</remarks>
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
        /// <summary>검증이 끝난 유물 시트 한 행을 이 서브에셋에 반영합니다.</summary>
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
}
