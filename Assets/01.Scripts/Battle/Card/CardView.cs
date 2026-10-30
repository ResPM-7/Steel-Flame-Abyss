using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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
    private BattleCombatantView previewTarget;
    private RectTransform cardCenter;
    private CardTargetArrowView targetArrow;
    private bool isAiming;
    private Vector2 pointerPosition;

    //손패 공통 타겟팅 위치와 화살표 받음
    public void SetTargetingReferences(RectTransform center, CardTargetArrowView arrow)
    {
        cardCenter = center;
        targetArrow = arrow;
    }

    //연결 완료된 단일 대상 카드만 중앙 고정 사용
    private bool UsesTargeting => Card != null && CardPlayController.RequiresEnemyTarget(Card) &&
        cardCenter != null && targetArrow != null;

    //포인터가 멈춰도 적 사망과 화면 배치 변경 반영
    private void LateUpdate()
    {
        if (!isDragging) return;
        bool cancelPressed = (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);
        if (cancelPressed || !cardPlayController.CanKeepSelecting(Card))
        {
            ClearTargeting();
            isDragging = false;
            canvasGroup.blocksRaycasts = true;
            hoverTween.CancelDrag();
            RefreshDescription();
            return;
        }
        if (isAiming) UpdateAim();
    }

    //고정 카드와 화살표 및 대상 설명 갱신
    private void UpdateAim()
    {
        hoverTween.PinTo(cardCenter);
        SetPreviewTarget(cardPlayController.FindTarget(pointerPosition));
        targetArrow.Show(hoverTween.CardRect, previewTarget != null ? previewTarget.TargetAnchor : null,
            pointerPosition, previewTarget != null);
    }

    //이전 강조 해제 후 새 대상과 설명 반영
    private void SetPreviewTarget(BattleCombatantView target)
    {
        if (previewTarget == target) return;
        if (previewTarget != null) previewTarget.SetTargetHighlight(false);
        previewTarget = target;
        if (isAiming && previewTarget != null) previewTarget.SetTargetHighlight(true);
        RefreshDescription();
    }

    //자신이 사용 중인 화살표와 대상 강조 정리
    private void ClearTargeting()
    {
        //타겟팅 종료 시 크기 1로 부드럽게 복구
        if (isAiming && hoverTween != null && hoverTween.CardRect != null)
            hoverTween.AnimateTargetScale(1f);
        if (isAiming && targetArrow != null) targetArrow.Hide();
        if (previewTarget != null) previewTarget.SetTargetHighlight(false);
        previewTarget = null;
        isAiming = false;
    }

    //비활성화 시 선택 표시와 입력 차단 복구
    private void OnDisable()
    {
        ClearTargeting();
        isDragging = false;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        if (hoverTween != null) hoverTween.ReturnImmediately();
    }

    public CardInstance Card { get; private set; }
    public override bool IsConfigured => base.IsConfigured && hoverTween != null;

    //카드 드래그에 필요한 캔버스와 입력 차단 기능 저장
    private void Awake()
    {
        if (hoverTween == null)
        {
            GameLogger.LogError("[손패 카드] Hover Tween을 연결하거나 목록용 CardListItemView를 사용해 주세요", this);
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
        previewTarget = null;
        cardPlayController = playController;
        hoverTween.SetDragReceiver(this);
        SetDisplay(card.Data, card.Cost, cardPlayController.GetDescription(card));
        gameObject.SetActive(true);
        hoverTween.Initialize(transform as RectTransform, hoverLayer);
        hoverTween.gameObject.SetActive(true);
    }

    //현재 힘과 약화 및 드래그 대상의 취약을 설명에 반영
    public void RefreshDescription()
    {
        if (Card == null || cardPlayController == null || descriptionText == null)
            return;

        descriptionText.text = cardPlayController.GetDescription(Card, isDragging ? previewTarget : null);
    }

    //카드를 호버 레이어에 고정하고 드래그 시작 위치 저장
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Card == null || cardPlayController == null || !hoverTween.BeginDrag())
            return;

        if (!cardPlayController.CanKeepSelecting(Card))
        {
            hoverTween.CancelDrag();
            return;
        }
        dragStartScreenY = eventData.pressPosition.y;
        pointerPosition = eventData.position;
        canvasGroup.blocksRaycasts = false;
        isDragging = true;
        UpdatePreviewTarget(eventData);
    }

    //마우스 이동량만큼 카드 위치 변경
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        pointerPosition = eventData.position;
        float scaleFactor = rootCanvas != null ? rootCanvas.scaleFactor : 1f;
        bool shouldAim = UsesTargeting && pointerPosition.y >= dragStartScreenY + playDragDistance * scaleFactor;
        if (shouldAim)
        {
            if (!isAiming)
            {
                SetPreviewTarget(null);
                //화살표 타겟팅 진입 시 크기 0.5로 축소
                hoverTween.AnimateTargetScale(0.5f);
            }
            isAiming = true;
            UpdateAim();
            return;
        }
        if (isAiming)
        {
            ClearTargeting();
            hoverTween.MoveToPointer(pointerPosition, eventData.pressEventCamera);
            RefreshDescription();
        }
        else hoverTween.MoveDuringDrag(eventData.delta, scaleFactor);
        UpdatePreviewTarget(eventData);
    }

    //포인터 대상 변경 시에만 설명 수치 갱신
    private void UpdatePreviewTarget(PointerEventData eventData)
    {
        SetPreviewTarget(UsesTargeting ? null : GetDropTarget(eventData));
    }

    //놓은 위치와 카드 대상 규칙으로 카드 사용 요청
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        pointerPosition = eventData.position;
        float scaleFactor = rootCanvas != null ? rootCanvas.scaleFactor : 1f;
        bool raisedEnough = eventData.position.y >= dragStartScreenY + playDragDistance * scaleFactor;
        BattleCombatantView targetView = UsesTargeting
            ? (raisedEnough ? cardPlayController.FindTarget(pointerPosition) : null)
            : GetDropTarget(eventData);
        ClearTargeting();
        isDragging = false;
        canvasGroup.blocksRaycasts = true;
        bool played = cardPlayController.TryPlayCard(Card, targetView, raisedEnough);
        if (!played)
        {
            hoverTween.CancelDrag();
            RefreshDescription();
        }
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
        ClearTargeting();
        isDragging = false;
        previewTarget = null;
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
