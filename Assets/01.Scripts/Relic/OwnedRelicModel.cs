using System;
using System.Collections.Generic;

//런 동안 보유한 유물 ID를 중복 없이 유지
public sealed class OwnedRelicModel
{
    private readonly List<int> ids = new List<int>();
    private readonly IReadOnlyList<int> readOnlyIds;
    public IReadOnlyList<int> RelicIds => readOnlyIds;
    public event Action Changed;

    public OwnedRelicModel() => readOnlyIds = ids.AsReadOnly();

    //데이터베이스에 존재하는 유물만 한 번 지급
    public bool TryAdd(int id, IGameDataProvider data)
    {
        if (data == null || !data.TryGetRelic(id, out _) || ids.Contains(id)) return false;
        ids.Add(id);
        Changed?.Invoke();
        return true;
    }

    //새 게임 시작 시 보유 목록 초기화
    public void Reset()
    {
        if (ids.Count == 0) return;
        ids.Clear();
        Changed?.Invoke();
    }
}
