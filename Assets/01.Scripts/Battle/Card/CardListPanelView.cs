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

    //초기 카드 목록 패널 숨김
    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    //목록 데이터와 오브젝트 풀 주입
    public void Inject(OwnedDeckModel ownedDeck, IGameDataProvider provider,
        IBattleDeckManager battleDeck, ObjectPoolManager objectPool)
    {
        if (ownedDeck == null || provider == null || battleDeck == null || objectPool == null)
        {
            Debug.LogError("[카드 목록] 덱과 데이터 공급자 및 오브젝트 풀을 연결해 주세요", this);
            return;
        }
        Close();
        if (deck != null)
            deck.Changed -= RefreshVisible;
        pool = objectPool;
        deck = battleDeck;
        ownedSource = new OwnedCardListSource(ownedDeck, provider);
        drawSource = new DrawCardListSource(deck);
        discardSource = new DiscardCardListSource(deck);
        deck.Changed += RefreshVisible;
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
            Debug.LogError("[카드 목록] 의존성 주입과 Panel 및 Title Text와 Scroll Rect 연결을 확인해 주세요", this);
            return;
        }
        currentSource = source;
        panel.SetActive(true);
        RefreshVisible();
        Canvas.ForceUpdateCanvases();
        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;
        scrollRect.horizontalNormalizedPosition = 0f;
    }

    //열린 목록의 카드 정보 갱신
    private void RefreshVisible()
    {
        if (currentSource == null || panel == null || !panel.activeInHierarchy)
            return;
        ReturnCards();
        List<CardData> cards = currentSource.GetCards();
        cards.Sort((left, right) => left.Id.CompareTo(right.Id));
        titleText.text = $"{currentSource.Title} ({cards.Count})";
        foreach (CardData card in cards)
        {
            GameObject item = pool.GetObject(poolName);
            if (item == null)
            {
                Debug.LogError($"[카드 목록] {poolName} 풀 등록과 초기화를 확인해 주세요", this);
                Close();
                return;
            }
            displayedCards.Add(item);
            CardListItemView view = item.GetComponent<CardListItemView>();
            if (view == null || !view.IsConfigured)
            {
                Debug.LogError("[카드 목록] 풀 프리팹의 CardListItemView와 표시 항목을 연결해 주세요", item);
                Close();
                return;
            }
            item.transform.SetParent(scrollRect.content, false);
            item.transform.SetAsLastSibling();
            view.Show(card);
        }
    }

    //표시 카드 반환과 목록 패널 닫기
    public void Close()
    {
        currentSource = null;
        ReturnCards();
        if (panel != null)
            panel.SetActive(false);
    }

    //사용한 카드 오브젝트를 풀로 반환
    private void ReturnCards()
    {
        foreach (GameObject item in displayedCards)
        {
            if (item != null && pool != null)
                pool.ReturnObject(poolName, item);
        }
        displayedCards.Clear();
    }

    //비활성화 시 목록 카드 반환
    private void OnDisable()
    {
        Close();
    }

    //전투 덱 이벤트 구독 해제
    private void OnDestroy()
    {
        if (deck != null)
            deck.Changed -= RefreshVisible;
        ReturnCards();
    }
}
