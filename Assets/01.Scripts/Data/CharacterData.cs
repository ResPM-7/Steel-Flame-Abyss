using System.Collections.Generic;
using UnityEngine;

namespace SteelFlameAbyss.Data
{
    /// <summary>플레이어 캐릭터의 기본 능력치와 카드 풀을 보관합니다.</summary>
    public sealed class CharacterData : GameDataEntry
    {
        [SerializeField] private CharacterClass characterClass;
        [Min(1)] [SerializeField] private int maxHealth = 1;
        [SerializeField] private List<int> startingDeckIds = new();
        [SerializeField] private List<int> cardPoolIds = new();
        [SerializeField] private string resourceName;
        [SerializeField] private Sprite portrait;

        public CharacterClass Class => characterClass;
        public int MaxHealth => maxHealth;
        public IReadOnlyList<int> StartingDeckIds => startingDeckIds;
        public IReadOnlyList<int> CardPoolIds => cardPoolIds;
        public string ResourceName => resourceName;
        public Sprite Portrait => portrait;

#if UNITY_EDITOR
        /// <summary>검증이 끝난 캐릭터 시트 한 행을 이 서브에셋에 반영합니다.</summary>
        public void EditorApply(CharacterClass newClass, int newMaxHealth, List<int> newStartingDeckIds,
            List<int> newCardPoolIds, string newResourceName)
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
