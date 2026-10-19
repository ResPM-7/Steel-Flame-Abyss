using System.Collections.Generic;
using TMPro;
using UnityEngine;

//공통 카드 목록 패널과 풀 오브젝트 관리
public class CardListPanelView : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private UnityEngine.UI.ScrollRect scrollRect;
    [SerializeField] private string poolName = "ViewCardList";

    private readonly List<GameObject> displayedCards = new List<GameObject>();
    private ObjectPoolManager pool;
    private IBattleDeckManager deck;
    private CardListSource ownedSource;
    private CardListSource drawSource;
    private CardListSource discardSource;
    private CardListSource currentSource;
    private int refreshVersion;
    public event System.Action<bool> VisibilityChanged;
    private bool isVisible;

    //초기 카드 목록 패널 숨김
    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    //보유 목록과 선택적 전투 목록 및 오브젝트 풀 주입
    public void Inject(OwnedDeckModel ownedDeck, IGameDataProvider provider,
        IBattleDeckManager battleDeck, ObjectPoolManager objectPool)
    {
        if (ownedDeck == null || provider == null || objectPool == null)
        {
            GameLogger.LogError("[카드 목록] 덱과 데이터 공급자 및 오브젝트 풀을 연결해 주세요", this);
            return;
        }
        Close();
        if (deck != null)
            deck.Changed -= RefreshVisible;
        pool = objectPool;
        deck = battleDeck;
        ownedSource = new OwnedCardListSource(ownedDeck, provider);
        drawSource = deck != null ? new DrawCardListSource(deck) : null;
        discardSource = deck != null ? new DiscardCardListSource(deck) : null;
        if (deck != null) deck.Changed += RefreshVisible;
    }

    //전체 보유 덱 목록 열기
    public void ShowOwnedDeck()
    {
        Open(ownedSource);
    }

    //현재 뽑기 더미 목록 열기
    public void ShowDrawPile()
    {
        Open(drawSource);
    }

    //현재 버린 더미 목록 열기
    public void ShowDiscardPile()
    {
        Open(discardSource);
    }

    //선택한 목록 표시와 스크롤 위치 초기화
    private void Open(CardListSource source)
    {
        if (source == null || pool == null || panel == null || titleText == null ||
            scrollRect == null || scrollRect.content == null)
        {
            GameLogger.LogError("[카드 목록] 의존성 주입과 Panel 및 Title Text와 Scroll Rect 연결을 확인해 주세요", this);
            return;
        }
        currentSource = source;
        panel.SetActive(true);
        NotifyVisibility(true);
        RefreshVisible();
        Canvas.ForceUpdateCanvases();
        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;
        scrollRect.horizontalNormalizedPosition = 0f;
    }

    //풀 로드 대기 후 최신 요청의 카드 목록 갱신
    private async void RefreshVisible()
    {
        if (currentSource == null || panel == null || !panel.activeInHierarchy)
            return;
        ReturnCards();
        int version = ++refreshVersion;
        var requestedPool = pool;
        bool ready = await requestedPool.EnsurePoolReadyAsync(poolName);
        if (this == null || version != refreshVersion || currentSource == null ||
            panel == null || !panel.activeInHierarchy || requestedPool == null) return;
        if (!ready)
        {
            titleText.text = "카드 로드 실패 — 닫은 후 다시 시도해 주세요";
            return;
        }
        List<CardData> cards = currentSource.GetCards();
        cards.Sort((left, right) => left.Id.CompareTo(right.Id));
        titleText.text = $"{currentSource.Title} ({cards.Count})";
        foreach (CardData card in cards)
        {
            GameObject item = pool.GetObject(poolName);
            if (item == null)
            {
                GameLogger.LogError($"[카드 목록] {poolName} 풀 등록과 초기화를 확인해 주세요", this);
                Close();
                return;
            }
            displayedCards.Add(item);
            CardListItemView view = item.GetComponent<CardListItemView>();
            if (view == null || !view.IsConfigured)
            {
                GameLogger.LogError("[카드 목록] 풀 프리팹의 CardListItemView와 표시 항목을 연결해 주세요", item);
                Close();
                return;
            }
            item.transform.SetParent(scrollRect.content, false);
            item.transform.SetAsLastSibling();
            view.Show(card);
        }
        Canvas.ForceUpdateCanvases();
        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;
        scrollRect.horizontalNormalizedPosition = 0f;
    }

    //표시 카드 반환과 목록 패널 닫기
    public void Close()
    {
        refreshVersion++;
        currentSource = null;
        ReturnCards(!isActiveAndEnabled || panel == null || !panel.activeInHierarchy);
        if (panel != null)
            panel.SetActive(false);
        NotifyVisibility(false);
    }

    //카드 목록 표시 상태 변경 알림
    private void NotifyVisibility(bool visible)
    {
        if (isVisible == visible) return;
        isVisible = visible;
        VisibilityChanged?.Invoke(visible);
    }

    //카드 소유 목록 제거 후 즉시 반환 또는 안전한 반환 예약
    private void ReturnCards(bool deferred = false)
    {
        while (displayedCards.Count > 0)
        {
            int index = displayedCards.Count - 1;
            GameObject item = displayedCards[index];
            displayedCards.RemoveAt(index);
            if (item != null && pool != null)
            {
                if (deferred) pool.RequestReturn(poolName, item);
                else pool.ReturnObject(poolName, item);
            }
        }
    }

    //부모 비활성화 중 재진입 없이 카드 반환 예약
    private void OnDisable()
    {
        refreshVersion++;
        currentSource = null;
        ReturnCards(true);
        NotifyVisibility(false);
    }

    //씬 제거 시 부모 이동 없이 전투 구독과 카드 참조 해제
    private void OnDestroy()
    {
        if (deck != null)
            deck.Changed -= RefreshVisible;
        displayedCards.Clear();
    }
}
