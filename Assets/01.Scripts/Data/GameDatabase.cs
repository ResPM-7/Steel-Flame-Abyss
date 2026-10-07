using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임에서 사용하는 정적 데이터를 한 파일에 모아 두는 루트 데이터베이스입니다.
/// 카드, 캐릭터, 적, 유물은 이 에셋의 서브에셋으로 저장됩니다.
/// </summary>
[CreateAssetMenu(fileName = "GameDatabase", menuName = "강철과 불꽃과 심연/게임 데이터베이스")]
public class GameDatabase : ScriptableObject
{
    [Header("동기화된 데이터 (서브에셋)")]
    [SerializeField] private List<CardData> cards = new();
    [SerializeField] private List<CharacterData> characters = new();
    [SerializeField] private List<EnemyData> enemies = new();
    [SerializeField] private List<RelicData> relics = new();

    private Dictionary<int, CardData> cardById;
    private Dictionary<int, CharacterData> characterById;
    private Dictionary<int, EnemyData> enemyById;
    private Dictionary<int, RelicData> relicById;

    public IReadOnlyList<CardData> Cards => cards;
    public IReadOnlyList<CharacterData> Characters => characters;
    public IReadOnlyList<EnemyData> Enemies => enemies;
    public IReadOnlyList<RelicData> Relics => relics;
    private void OnEnable() => RebuildLookup();

    public bool TryGetCard(int id, out CardData value)
    {
        EnsureLookup();
        return cardById.TryGetValue(id, out value);
    }

    public bool TryGetCharacter(int id, out CharacterData value)
    {
        EnsureLookup();
        return characterById.TryGetValue(id, out value);
    }

    public bool TryGetEnemy(int id, out EnemyData value)
    {
        EnsureLookup();
        return enemyById.TryGetValue(id, out value);
    }

    public bool TryGetRelic(int id, out RelicData value)
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
        // 런타임에서는 정수 ID로 빠르게 데이터를 찾을 수 있도록 사전을 구성합니다.
        cardById = BuildLookup(cards);
        characterById = BuildLookup(characters);
        enemyById = BuildLookup(enemies);
        relicById = BuildLookup(relics);
    }

    private static Dictionary<int, T> BuildLookup<T>(IEnumerable<T> entries) where T : GameDataEntry
    {
        var result = new Dictionary<int, T>();
        foreach (var entry in entries)
        {
            if (entry != null && entry.Id > 0)
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
