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
        private const string SpreadsheetId = "1z6twBTcf1Y_HKrEMNkVUokgKD0W2Fwu4a7d54ise_Ls";
        private const string CardsDefaultUrl = "https://docs.google.com/spreadsheets/d/" + SpreadsheetId + "/export?format=csv&gid=0";
        private const string CharactersDefaultUrl = "https://docs.google.com/spreadsheets/d/" + SpreadsheetId + "/export?format=csv&gid=101001";
        private const string EnemiesDefaultUrl = "https://docs.google.com/spreadsheets/d/" + SpreadsheetId + "/export?format=csv&gid=101002";

        [Header("원격 CSV 주소")]
        [Tooltip("카드 시트의 CSV 공개/내보내기 주소")]
        [SerializeField] private string cardsCsvUrl = CardsDefaultUrl;
        [Tooltip("캐릭터 시트의 CSV 공개/내보내기 주소")]
        [SerializeField] private string charactersCsvUrl = CharactersDefaultUrl;
        [Tooltip("적 시트의 CSV 공개/내보내기 주소")]
        [SerializeField] private string enemiesCsvUrl = EnemiesDefaultUrl;
        [Tooltip("선택 사항입니다. 유물 시트가 없으면 비워 두세요.")]
        [SerializeField] private string relicsCsvUrl;

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
        // 기존 에셋에 새 URL 필드가 아직 직렬화되지 않았어도 프로젝트 기본 시트로 동작합니다.
        public string CardsCsvUrl => string.IsNullOrWhiteSpace(cardsCsvUrl) ? CardsDefaultUrl : cardsCsvUrl;
        public string CharactersCsvUrl => string.IsNullOrWhiteSpace(charactersCsvUrl) ? CharactersDefaultUrl : charactersCsvUrl;
        public string EnemiesCsvUrl => string.IsNullOrWhiteSpace(enemiesCsvUrl) ? EnemiesDefaultUrl : enemiesCsvUrl;
        public string RelicsCsvUrl => relicsCsvUrl;
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
        /// <summary>에디터 동기화 도구가 검증·정규화한 공개 CSV 주소를 저장합니다.</summary>
        public void EditorSetCsvUrls(string newCardsUrl, string newCharactersUrl,
            string newEnemiesUrl, string newRelicsUrl)
        {
            cardsCsvUrl = newCardsUrl;
            charactersCsvUrl = newCharactersUrl;
            enemiesCsvUrl = newEnemiesUrl;
            relicsCsvUrl = newRelicsUrl;
        }

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
