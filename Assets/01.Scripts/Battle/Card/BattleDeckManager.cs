using System;
using System.Collections.Generic;
using UnityEngine;

public interface IBattleDeckManager
{
    event Action Changed;

    bool IsReady { get; }
    int HandCount { get; }
    int DrawCount { get; }
    int DiscardCount { get; }
    IReadOnlyList<CardInstance> Hand { get; }

    //카드 ID로 전투 덱 생성
    bool InitializeDeck(IEnumerable<int> cardIds);

    //테스트 카드 ID로 전투 덱과 초기 손패 생성
    bool ResetTestDeck();

    //지정한 수만큼 카드를 손패로 이동
    int DrawCards(int count);

    //지정한 손패 순번의 카드를 버린 더미로 이동
    bool DiscardAt(int index);

    //현재 손패 전체를 버린 더미로 이동
    void DiscardAll();

    //카드 ID를 조회해 손패에 바로 추가
    bool TryAddCardToHand(int cardId, out CardInstance card);
}

//현재 전투의 카드 더미와 손패 상태를 관리
public class BattleDeckManager : MonoBehaviour, IBattleDeckManager
{
    [Header("임시 테스트 덱")]
    [SerializeField] private int[] testDeckIds =
    {
        10001, 10001, 10001, 10001, 10001,
        10002, 10002, 10002, 10002, 10002, 10003, 10003
    };
    [Min(0)] [SerializeField] private int initialHandCount = 3;

    private IGameDataProvider dataProvider;
    private DeckState deck;

    public event Action Changed;
    public DeckState Deck => deck;
    public bool IsReady => deck != null;
    public int HandCount => deck?.Hand.Count ?? 0;
    public int DrawCount => deck?.DrawPile.Count ?? 0;
    public int DiscardCount => deck?.DiscardPile.Count ?? 0;
    public IReadOnlyList<CardInstance> Hand =>
        deck != null ? deck.Hand : Array.Empty<CardInstance>();

    //외부에서 전달한 데이터 조회 기능 저장
    public void Inject(IGameDataProvider provider)
    {
        if (provider == null)
            throw new System.ArgumentNullException(nameof(provider));

        dataProvider = provider;
    }

    //카드 ID로 전투 덱 생성
    public bool InitializeDeck(IEnumerable<int> cardIds)
    {
        if (dataProvider == null || !dataProvider.IsReady)
        {
            Debug.LogError("[덱] 사용 가능한 데이터 공급자를 먼저 주입해 주세요", this);
            return false;
        }

        if (cardIds == null)
            throw new System.ArgumentNullException(nameof(cardIds));

        var cards = new List<CardInstance>();

        foreach (int cardId in cardIds)
        {
            if (!dataProvider.TryGetCard(cardId, out CardData data))
            {
                Debug.LogError($"[덱] 카드 ID {cardId}가 없어 덱 생성을 취소했습니다", this);
                return false;
            }

            cards.Add(new CardInstance(data));
        }

        deck = new DeckState(cards);
        Changed?.Invoke();
        return true;
    }

    //테스트 카드 ID로 전투 덱과 초기 손패 생성
    public bool ResetTestDeck()
    {
        if (testDeckIds == null || testDeckIds.Length == 0)
        {
            Debug.LogError("[덱] 테스트 카드 ID를 한 장 이상 등록해 주세요", this);
            return false;
        }

        if (!InitializeDeck(testDeckIds))
            return false;

        DrawCards(initialHandCount);
        return true;
    }

    //지정한 수만큼 카드를 손패로 이동
    public int DrawCards(int count)
    {
        if (!IsReady)
            return 0;

        int drawnCount = deck.Draw(count).Count;
        if (drawnCount > 0)
            Changed?.Invoke();

        return drawnCount;
    }

    //지정한 손패 순번의 카드를 버린 더미로 이동
    public bool DiscardAt(int index)
    {
        if (!IsReady || index < 0 || index >= HandCount)
            return false;

        bool discarded = deck.Discard(deck.Hand[index]);
        if (discarded)
            Changed?.Invoke();

        return discarded;
    }

    //현재 손패 전체를 버린 더미로 이동
    public void DiscardAll()
    {
        if (!IsReady || HandCount == 0)
            return;

        deck.DiscardHand();
        Changed?.Invoke();
    }

    //카드 ID를 조회해 손패에 바로 추가
    public bool TryAddCardToHand(int cardId, out CardInstance card)
    {
        card = null;
        if (!IsReady || dataProvider == null || !dataProvider.IsReady)
            return false;
        if (!dataProvider.TryGetCard(cardId, out CardData data))
            return false;

        card = new CardInstance(data);
        deck.AddToHand(card);
        Changed?.Invoke();
        return true;
    }
}
