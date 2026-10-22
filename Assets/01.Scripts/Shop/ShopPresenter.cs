using System;
using System.Collections.Generic;

//상점 지점별 상품 보관과 구매 및 화면 연결
public class ShopPresenter : IDisposable
{
    private readonly CoinModel coins;
    private readonly OwnedDeckModel deck;
    private readonly ShopPanelView view;
    private readonly RunProgressModel run;
    private readonly OwnedRelicModel relics;
    private readonly IGameDataProvider data;
    private readonly Random random = new Random();
    private ShopModel current;
    private Action completed;
    private bool open;

    //코인과 보유 덱 및 상점 화면 의존성 연결
    public ShopPresenter(CoinModel coins, OwnedDeckModel deck, ShopPanelView view,
        OwnedRelicModel relics, IGameDataProvider data, RunProgressModel run)
    {
        this.coins = coins ?? throw new ArgumentNullException(nameof(coins));
        this.deck = deck ?? throw new ArgumentNullException(nameof(deck));
        this.view = view ?? throw new ArgumentNullException(nameof(view));
        this.relics = relics ?? throw new ArgumentNullException(nameof(relics));
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.run = run ?? throw new ArgumentNullException(nameof(run));
        coins.Changed += Refresh;
        view.BuyRequested += Buy;
        view.LeaveRequested += Leave;
        view.RelicBuyRequested += BuyRelic;
        relics.Changed += RefreshRelics;
    }

    //지점 첫 방문의 상품 생성과 기존 상품 재표시
    public void Open(int nodeIndex, IEnumerable<CardData> cards, CharacterClass owner, Action onCompleted)
    {
        current = run.GetShopVisit(nodeIndex, () =>
        {
            var model = new ShopModel(cards, owner, random, view.GetCardPrice);
            model.InitializeRelics(data.Database.Relics, relics, random, view.RelicPrices, view.RelicOfferCount);
            return model;
        });
        completed = onCompleted;
        open = true;
        view.Show(current, coins.Amount, relics);
    }

    //유물 구매 요청과 보유 목록 변경 반영
    private void BuyRelic(int index)
    {
        if (!open || !view.isActiveAndEnabled) return;
        current.TryBuyRelic(index, coins, relics, data);
        Refresh(coins.Amount);
    }

    //외부 유물 지급 시 구매 상태 갱신
    private void RefreshRelics() => Refresh(coins.Amount);

    //현재 상점의 카드 구매와 화면 갱신
    private void Buy(int index)
    {
        if (!open || !view.isActiveAndEnabled || !current.TryBuy(index, coins, deck))
            return;
        Refresh(coins.Amount);
    }

    //보유 코인 변경에 따른 구매 상태 갱신
    private void Refresh(int amount)
    {
        if (open && view.isActiveAndEnabled)
            view.Refresh(current, amount, relics);
    }

    //구매 여부와 관계없는 상점 완료 처리
    private void Leave()
    {
        if (!open)
            return;
        open = false;
        view.Close();
        Action callback = completed;
        completed = null;
        callback?.Invoke();
    }

    //코인과 상점 입력 이벤트 구독 해제
    public void Dispose()
    {
        open = false;
        coins.Changed -= Refresh;
        view.BuyRequested -= Buy;
        view.LeaveRequested -= Leave;
        view.RelicBuyRequested -= BuyRelic;
        relics.Changed -= RefreshRelics;
        completed = null;
    }
}
