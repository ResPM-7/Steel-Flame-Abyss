using System;
using System.Collections.Generic;
using UnityEngine;

namespace SteelFlameAbyss.Data
{
    /// <summary>
    /// 게임에서 사용하는 정적 데이터를 한 파일에 모아 두는 루트 데이터베이스입니다.
    /// 카드, 캐릭터, 적, 유물은 이 에셋의 서브에셋으로 저장됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "GameDatabase", menuName = "강철과 불꽃과 심연/게임 데이터베이스")]
    public sealed class GameDatabase : ScriptableObject
    {
        [Header("Google 시트 업로드")]
        [Tooltip("Google Apps Script를 웹앱으로 배포한 실행 URL입니다.")]
        [SerializeField] private string sheetUploadUrl;
        [SerializeField] private string cardsSheetName = "Total Card";
        [SerializeField] private string charactersSheetName = "Character";
        [SerializeField] private string enemiesSheetName = "Enemy";
        [SerializeField] private string relicsSheetName = "Relic";

        [Header("동기화된 데이터 (서브에셋)")]
        [SerializeField] private List<CardData> cards = new();
        [SerializeField] private List<CharacterData> characters = new();
        [SerializeField] private List<EnemyData> enemies = new();
        [SerializeField] private List<RelicData> relics = new();

        private Dictionary<string, CardData> cardById;
        private Dictionary<string, CharacterData> characterById;
        private Dictionary<string, EnemyData> enemyById;
        private Dictionary<string, RelicData> relicById;

        public IReadOnlyList<CardData> Cards => cards;
        public IReadOnlyList<CharacterData> Characters => characters;
        public IReadOnlyList<EnemyData> Enemies => enemies;
        public IReadOnlyList<RelicData> Relics => relics;
        public string SheetUploadUrl => sheetUploadUrl;
        public string CardsSheetName => cardsSheetName;
        public string CharactersSheetName => charactersSheetName;
        public string EnemiesSheetName => enemiesSheetName;
        public string RelicsSheetName => relicsSheetName;

        private void OnEnable() => RebuildLookup();

        public bool TryGetCard(string id, out CardData value)
        {
            EnsureLookup();
            return cardById.TryGetValue(id, out value);
        }

        public bool TryGetCharacter(string id, out CharacterData value)
        {
            EnsureLookup();
            return characterById.TryGetValue(id, out value);
        }

        public bool TryGetEnemy(string id, out EnemyData value)
        {
            EnsureLookup();
            return enemyById.TryGetValue(id, out value);
        }

        public bool TryGetRelic(string id, out RelicData value)
        {
            EnsureLookup();
            return relicById.TryGetValue(id, out value);
        }

        private void EnsureLookup()
        {
            if (cardById == null)
                RebuildLookup();
        }

        private void RebuildLookup()
        {
            // 런타임에서는 문자열 ID로 빠르게 데이터를 찾을 수 있도록 사전을 구성합니다.
            cardById = BuildLookup(cards);
            characterById = BuildLookup(characters);
            enemyById = BuildLookup(enemies);
            relicById = BuildLookup(relics);
        }

        private static Dictionary<string, T> BuildLookup<T>(IEnumerable<T> entries) where T : GameDataEntry
        {
            var result = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Id))
                    result[entry.Id] = entry;
            }
            return result;
        }

#if UNITY_EDITOR
        public void EditorSetEntries(List<CardData> newCards, List<CharacterData> newCharacters,
            List<EnemyData> newEnemies, List<RelicData> newRelics)
        {
            cards = newCards;
            characters = newCharacters;
            enemies = newEnemies;
            relics = newRelics;
            RebuildLookup();
        }
#endif
    }
}
