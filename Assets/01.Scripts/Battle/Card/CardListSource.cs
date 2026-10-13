using System.Collections.Generic;

//카드 목록별 제목과 조회 방식 정의
public abstract class CardListSource
{
    public abstract string Title { get; }

    //표시할 카드 목록 조회
    public abstract List<CardData> GetCards();
}

//보상 카드를 포함한 보유 덱 조회
public class OwnedCardListSource : CardListSource
{
    private readonly OwnedDeckModel deck;
    private readonly IGameDataProvider provider;
    public override string Title => "덱 목록";

    //보유 덱과 데이터 공급자 저장
    public OwnedCardListSource(OwnedDeckModel deck, IGameDataProvider provider)
    {
        this.deck = deck;
        this.provider = provider;
    }

    //보유 카드 ID를 표시 데이터로 변환
    public override List<CardData> GetCards()
    {
        var cards = new List<CardData>();
        foreach (int id in deck.CardIds)
        {
            if (provider.TryGetCard(id, out CardData card))
                cards.Add(card);
        }
        return cards;
    }
}

//현재 뽑기 더미 조회
public class DrawCardListSource : CardListSource
{
    private readonly IBattleDeckManager deck;
    public override string Title => "뽑기 더미";

    //전투 덱 조회 기능 저장
    public DrawCardListSource(IBattleDeckManager deck)
    {
        this.deck = deck;
    }

    //뽑기 더미의 표시용 카드 목록 복사
    public override List<CardData> GetCards()
    {
        var cards = new List<CardData>();
        foreach (CardInstance card in deck.DrawPile)
            cards.Add(card.Data);
        return cards;
    }
}

//현재 버린 더미 조회
public class DiscardCardListSource : CardListSource
{
    private readonly IBattleDeckManager deck;
    public override string Title => "버린 더미";

    //전투 덱 조회 기능 저장
    public DiscardCardListSource(IBattleDeckManager deck)
    {
        this.deck = deck;
    }

    //버린 더미의 표시용 카드 목록 복사
    public override List<CardData> GetCards()
    {
        var cards = new List<CardData>();
        foreach (CardInstance card in deck.DiscardPile)
            cards.Add(card.Data);
        return cards;
    }
}
