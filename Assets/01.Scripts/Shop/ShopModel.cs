using System;
using System.Collections.Generic;

//상점 지점별 카드 상품과 가격 및 품절 상태 관리
public class ShopModel
{
    private readonly List<CardData> cards = new List<CardData>();
    private readonly HashSet<int> sold = new HashSet<int>();
    private bool purchasing;
    public IReadOnlyList<CardData> Cards => cards.AsReadOnly();
    public int Price { get; }

    //캐릭터와 공용 카드에서 중복 없는 상품 세 장 추첨
    public ShopModel(IEnumerable<CardData> source, CharacterClass owner, Random random, int price)
    {
        if (source == null || random == null)
            throw new ArgumentNullException();
        if (price <= 0)
            throw new ArgumentOutOfRangeException(nameof(price));
        Price = price;
        var pool = new List<CardData>();
        var ids = new HashSet<int>();
        foreach (CardData card in source)
            if (card != null && card.Id > 0 && card.CardType != CardType.Status &&
                (card.Owner == owner || card.Owner == CharacterClass.Common) && ids.Add(card.Id))
                pool.Add(card);
        while (pool.Count > 0 && cards.Count < 3)
        {
            int index = random.Next(pool.Count);
            cards.Add(pool[index]);
            pool.RemoveAt(index);
        }
    }

    //상품 순번의 품절 여부 조회
    public bool IsSold(int index) => sold.Contains(index);

    //구매 조건 확인과 코인 차감 및 보유 덱 추가
    public bool TryBuy(int index, CoinModel coins, OwnedDeckModel deck)
    {
        if (purchasing || coins == null || deck == null || !deck.IsInitialized ||
            index < 0 || index >= cards.Count || sold.Contains(index) || coins.Amount < Price)
            return false;
        purchasing = true;
        sold.Add(index);
        try
        {
            if (!coins.TrySpend(Price))
            {
                sold.Remove(index);
                return false;
            }
            deck.Add(cards[index].Id);
            return true;
        }
        finally
        {
            purchasing = false;
        }
    }
}
