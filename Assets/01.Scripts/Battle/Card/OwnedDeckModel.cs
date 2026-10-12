using System;
using System.Collections.Generic;

//게임 진행 동안 보유 카드 목록 유지
public class OwnedDeckModel
{
    private readonly List<int> cards = new List<int>();
    public IReadOnlyList<int> CardIds => cards.AsReadOnly();
    public bool IsInitialized { get; private set; }

    //첫 전투의 시작 카드 목록 저장
    public void InitializeOnce(IEnumerable<int> cardIds)
    {
        if (IsInitialized)
            return;
        if (cardIds == null)
            throw new ArgumentNullException(nameof(cardIds));
        cards.AddRange(cardIds);
        IsInitialized = true;
    }

    //선택한 보상 카드를 보유 덱에 추가
    public void Add(int cardId)
    {
        if (!IsInitialized || cardId <= 0)
            throw new InvalidOperationException("보유 덱 초기화와 카드 ID를 확인해 주세요");
        cards.Add(cardId);
    }

    //새 게임의 보유 덱 초기화
    public void Reset()
    {
        cards.Clear();
        IsInitialized = false;
    }
}
