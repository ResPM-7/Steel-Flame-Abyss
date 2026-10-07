using System;
using System.Collections.Generic;
using UnityEngine;

//게임 시스템이 정적 SO 데이터를 조회할 때 사용하는 공용 인터페이스
public interface IGameDataProvider
{
    GameDatabase Database { get; }
    bool IsReady { get; }

    bool TryGetCard(int id, out CardData value);
    bool TryGetCharacter(int id, out CharacterData value);
    bool TryGetEnemy(int id, out EnemyData value);
    bool TryGetRelic(int id, out RelicData value);

    CardData GetCard(int id);
    CharacterData GetCharacter(int id);
    EnemyData GetEnemy(int id);
    RelicData GetRelic(int id);
}

//GameDatabase SO를 직접 노출하지 않고 카드·캐릭터·적·유물 조회를 한곳에서 제공
//사용하는 시스템은 이 컴포넌트를 명시적으로 참조해 데이터 의존성을 전달
[DisallowMultipleComponent]
public class GameDataProvider : MonoBehaviour, IGameDataProvider
{
    [Tooltip("시트에서 동기화된 GameDatabase 에셋")]
    [SerializeField] private GameDatabase database;

    public GameDatabase Database => database;
    public bool IsReady => database != null;

    private void Awake()
    {
        if (database == null)
            Debug.LogError("[게임 데이터] GameDataProvider에 GameDatabase가 지정되지 않았습니다.", this);
    }

    public bool TryGetCard(int id, out CardData value)
    {
        value = null;
        return database != null && database.TryGetCard(id, out value);
    }

    public bool TryGetCharacter(int id, out CharacterData value)
    {
        value = null;
        return database != null && database.TryGetCharacter(id, out value);
    }

    public bool TryGetEnemy(int id, out EnemyData value)
    {
        value = null;
        return database != null && database.TryGetEnemy(id, out value);
    }

    public bool TryGetRelic(int id, out RelicData value)
    {
        value = null;
        return database != null && database.TryGetRelic(id, out value);
    }

    public CardData GetCard(int id) => GetRequired<CardData>(id, TryGetCard, "카드");
    public CharacterData GetCharacter(int id) =>
        GetRequired<CharacterData>(id, TryGetCharacter, "캐릭터");
    public EnemyData GetEnemy(int id) => GetRequired<EnemyData>(id, TryGetEnemy, "적");
    public RelicData GetRelic(int id) => GetRequired<RelicData>(id, TryGetRelic, "유물");

    private T GetRequired<T>(int id, TryGetData<T> tryGet, string dataType) where T : GameDataEntry
    {
        if (database == null)
            throw new InvalidOperationException("GameDataProvider에 GameDatabase가 지정되지 않았습니다.");
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), id, "데이터 ID는 0보다 커야 합니다.");
        if (tryGet(id, out var value))
            return value;
        throw new KeyNotFoundException($"{dataType} ID '{id}'를 GameDatabase에서 찾을 수 없습니다.");
    }

    private delegate bool TryGetData<T>(int id, out T value) where T : GameDataEntry;
}
