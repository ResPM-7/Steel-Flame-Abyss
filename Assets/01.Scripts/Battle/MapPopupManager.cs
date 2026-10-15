using System;
using UnityEngine;

//맵 팝업의 최초 생성과 데이터 주입 및 씬 내 재사용
public class MapPopupManager : MonoBehaviour
{
    [SerializeField] private Transform popupRoot;
    [SerializeField] private ShopPanelView shopPrefab;
    [SerializeField] private CampPanelView campPrefab;
    [SerializeField] private EventPanelView eventPrefab;
    [SerializeField] private CardListPanelView deckPrefab;
    private ShopPanelView shop;
    private CampPanelView camp;
    private EventPanelView eventView;
    private CardListPanelView deck;
    private ShopPresenter shopPresenter;
    private CoinModel coins;
    private OwnedDeckModel cards;
    private RunProgressModel run;
    private IGameDataProvider provider;
    private ObjectPoolManager pool;
    public bool IsConfigured => popupRoot != null && shopPrefab != null && shopPrefab.IsConfigured &&
        campPrefab != null && campPrefab.IsConfigured && eventPrefab != null && eventPrefab.IsConfigured && deckPrefab != null;

    //생성 없이 팝업에 필요한 세션 의존성 보관
    public void Inject(RunProgressModel progress, CoinModel coinModel, OwnedDeckModel ownedDeck,
        IGameDataProvider data, ObjectPoolManager objectPool)
    {
        if (run != null) throw new InvalidOperationException("팝업 관리자는 씬에서 한 번만 주입해 주세요");
        run = progress ?? throw new ArgumentNullException(nameof(progress));
        coins = coinModel ?? throw new ArgumentNullException(nameof(coinModel));
        cards = ownedDeck ?? throw new ArgumentNullException(nameof(ownedDeck));
        provider = data ?? throw new ArgumentNullException(nameof(data));
        pool = objectPool != null ? objectPool : throw new ArgumentNullException(nameof(objectPool));
    }

    //요청한 화면 최초 생성과 이후 동일 인스턴스 반환
    private T GetOrCreate<T>(ref T instance, T prefab) where T : Component
    {
        if (instance != null) return instance;
        instance = Instantiate(prefab, popupRoot, false);
        instance.name = prefab.name;
        instance.transform.SetAsFirstSibling();
        //비활성 프리팹의 Awake를 데이터 주입 전에 실행
        instance.gameObject.SetActive(true);
        instance.gameObject.SetActive(false);
        return instance;
    }

    //상점 화면 최초 연결과 지점별 상품 표시
    public void OpenShop(int nodeIndex, Action completed)
    {
        CloseAll();
        GetOrCreate(ref shop, shopPrefab);
        if (shopPresenter == null) shopPresenter = new ShopPresenter(coins, cards, shop);
        shopPresenter.Open(nodeIndex, provider.Database.Cards,
            provider.GetCharacter(run.PlayerId).Class, completed);
    }

    //캠프 화면 재사용과 새 방문 회복 상태 설정
    public void OpenCamp(Action completed)
    {
        CloseAll();
        GetOrCreate(ref camp, campPrefab).Open(run.Health, completed);
    }

    //이벤트 화면 재사용과 현재 방문 정보 전달
    public void OpenEvent(int nodeIndex, Action completed)
    {
        CloseAll();
        GetOrCreate(ref eventView, eventPrefab).Open(nodeIndex, completed);
    }

    //덱 목록 최초 주입과 최신 보유 카드 표시
    public void ShowOwnedDeck()
    {
        if (run == null) return;
        if (deck == null)
        {
            GetOrCreate(ref deck, deckPrefab);
            deck.Inject(cards, provider, null, pool);
        }
        deck.transform.SetAsLastSibling();
        deck.ShowOwnedDeck();
    }

    //생성된 덱 목록만 닫기
    public void CloseDeck()
    {
        if (deck != null) deck.Close();
    }

    //생성 없이 열린 팝업 비활성화
    public void CloseAll()
    {
        CloseDeck();
        if (shop != null) shop.Close();
        if (camp != null) camp.gameObject.SetActive(false);
        if (eventView != null) eventView.gameObject.SetActive(false);
    }

    //씬 해제 시 상점 모델 이벤트 구독 해제
    private void OnDestroy()
    {
        shopPresenter?.Dispose();
    }
}
