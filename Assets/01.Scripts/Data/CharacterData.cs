using System.Collections.Generic;
using UnityEngine;

namespace SteelFlameAbyss.Data
{
    /// <summary>플레이어 캐릭터의 기본 능력치와 카드 풀을 보관합니다.</summary>
    public sealed class CharacterData : GameDataEntry
    {
        [SerializeField] private CharacterClass characterClass;
        [Min(1)] [SerializeField] private int maxHealth = 1;
        [SerializeField] private List<string> startingDeckIds = new();
        [SerializeField] private List<string> cardPoolIds = new();
        [SerializeField] private string resourceName;
        [SerializeField] private Sprite portrait;

        public CharacterClass Class => characterClass;
        public int MaxHealth => maxHealth;
        public IReadOnlyList<string> StartingDeckIds => startingDeckIds;
        public IReadOnlyList<string> CardPoolIds => cardPoolIds;
        public string ResourceName => resourceName;
        public Sprite Portrait => portrait;

#if UNITY_EDITOR
        /// <summary>검증이 끝난 캐릭터 시트 한 행을 이 서브에셋에 반영합니다.</summary>
        public void EditorApply(CharacterClass newClass, int newMaxHealth, List<string> newStartingDeckIds,
            List<string> newCardPoolIds, string newResourceName)
        {
            characterClass = newClass;
            maxHealth = newMaxHealth;
            startingDeckIds = newStartingDeckIds;
            cardPoolIds = newCardPoolIds;
            resourceName = newResourceName;
        }
#endif
    }
}
