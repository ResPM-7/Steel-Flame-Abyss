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
    private readonly List<RelicOffer> relics = new List<RelicOffer>();
    private bool relicsInitialized;
    public IReadOnlyList<RelicOffer> Relics => relics.AsReadOnly();

    //유물 상품의 데이터와 확정 가격 및 품절 상태
    public class RelicOffer
    {
        public RelicData Data { get; }
        public int Price { get; }
        public bool Sold { get; internal set; }
        public RelicOffer(RelicData data, int price) { Data = data; Price = price; }
    }

    //첫 방문의 미보유 활성 유물 추첨과 가격 고정
    public void InitializeRelics(IEnumerable<RelicData> source, OwnedRelicModel owned,
        Random random, RelicShopPrices prices, int count)
    {
        if (relicsInitialized) return;
        if (source == null || owned == null || random == null || prices == null) throw new ArgumentNullException();
        var pool = new List<RelicData>();
        var ids = new HashSet<int>();
        foreach (RelicData relic in source)
            if (relic != null && relic.Id > 0 && relic.EffectsEnabled && !owned.Contains(relic.Id) && ids.Add(relic.Id))
                pool.Add(relic);
        while (pool.Count > 0 && relics.Count < count)
        {
            int index = random.Next(pool.Count);
            relics.Add(new RelicOffer(pool[index], prices.GetPrice(pool[index].Rarity)));
            pool.RemoveAt(index);
        }
        relicsInitialized = true;
    }

    //코인과 보유 상태 재확인 후 유물 구매 및 실패 환불
    public bool TryBuyRelic(int index, CoinModel coins, OwnedRelicModel owned, IGameDataProvider data)
    {
        if (purchasing || coins == null || owned == null || data == null || index < 0 || index >= relics.Count) return false;
        RelicOffer offer = relics[index];
        if (offer.Sold || offer.Data == null || !offer.Data.EffectsEnabled || owned.Contains(offer.Data.Id) ||
            coins.Amount < offer.Price || !data.TryGetRelic(offer.Data.Id, out _)) return false;
        purchasing = true;
        offer.Sold = true;
        try
        {
            if (!coins.TrySpend(offer.Price)) { offer.Sold = false; return false; }
            if (owned.TryAdd(offer.Data.Id, data)) return true;
            offer.Sold = false;
            coins.TryAdd(offer.Price);
            return false;
        }
        finally { purchasing = false; }
    }

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
