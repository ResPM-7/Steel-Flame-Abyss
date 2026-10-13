using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

//카드 데이터 표시와 마우스 드래그 입력 전달
public class CardView : CardDisplayView
{
    [SerializeField] private CardHoverTween hoverTween;
    [Min(1f)] [SerializeField] private float playDragDistance = 160f;

    private CardPlayController cardPlayController;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private float dragStartScreenY;
    private bool isDragging;

    public CardInstance Card { get; private set; }
    public override bool IsConfigured => base.IsConfigured && hoverTween != null;

    //카드 드래그에 필요한 캔버스와 입력 차단 기능 저장
    private void Awake()
    {
        if (hoverTween == null)
        {
            Debug.LogError("[손패 카드] Hover Tween을 연결하거나 목록용 CardListItemView를 사용해 주세요", this);
            return;
        }
        rootCanvas = GetComponentInParent<Canvas>();
        canvasGroup = hoverTween.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = hoverTween.gameObject.AddComponent<CanvasGroup>();
    }

    //전달받은 런타임 카드 정보를 표시하고 슬롯과 카드 사용 기능 연결
    public void Bind(CardInstance card, RectTransform hoverLayer, CardPlayController playController)
    {
        if (card == null)
            throw new System.ArgumentNullException(nameof(card));
        if (playController == null)
            throw new System.ArgumentNullException(nameof(playController));

        Card = card;
        cardPlayController = playController;
        hoverTween.SetDragReceiver(this);
        SetDisplay(card.Data, card.Cost, cardPlayController.GetDescription(card));
        gameObject.SetActive(true);
        hoverTween.Initialize(transform as RectTransform, hoverLayer);
        hoverTween.gameObject.SetActive(true);
    }

    //카드 위치를 유지하며 현재 힘을 설명에 반영
    public void RefreshDescription()
    {
        if (Card == null || cardPlayController == null || descriptionText == null)
            return;

        descriptionText.text = cardPlayController.GetDescription(Card);
    }

    //카드를 호버 레이어에 고정하고 드래그 시작 위치 저장
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Card == null || cardPlayController == null || !hoverTween.BeginDrag())
            return;

        dragStartScreenY = eventData.pressPosition.y;
        canvasGroup.blocksRaycasts = false;
        isDragging = true;
    }

    //마우스 이동량만큼 카드 위치 변경
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        float scaleFactor = rootCanvas != null ? rootCanvas.scaleFactor : 1f;
        hoverTween.MoveDuringDrag(eventData.delta, scaleFactor);
    }

    //놓은 위치와 카드 대상 규칙으로 카드 사용 요청
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        isDragging = false;
        canvasGroup.blocksRaycasts = true;

        BattleCombatantView targetView = GetDropTarget(eventData);
        bool raisedEnough = eventData.position.y >= dragStartScreenY + playDragDistance;
        bool played = cardPlayController.TryPlayCard(Card, targetView, raisedEnough);
        if (!played)
            hoverTween.CancelDrag();
    }

    //포인터 아래의 전투 참가자 UI 확인
    private static BattleCombatantView GetDropTarget(PointerEventData eventData)
    {
        if (eventData.pointerCurrentRaycast.gameObject == null)
            return null;

        return eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<BattleCombatantView>();
    }

    //호버 중인 카드도 원래 슬롯으로 복귀시킨 다음 슬롯을 숨김
    public void Hide()
    {
        isDragging = false;
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;
        hoverTween.ReturnImmediately();
        hoverTween.gameObject.SetActive(false);
        Card = null;
        cardPlayController = null;
        nameText.text = string.Empty;
        costText.text = string.Empty;
        descriptionText.text = string.Empty;
        gameObject.SetActive(false);
    }
}
