using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

//상점 카드 표시와 구매 입력 및 호버 확대 처리
public class ShopCardItemView : CardDisplayView, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UnityEngine.UI.Button button;
    [SerializeField] private TMP_Text priceText;
    [Header("카드 앞면과 뒷면")]
    [Tooltip("슬롯 루트와 가격 텍스트를 제외한 앞면 자식 오브젝트")]
    [SerializeField] private GameObject cardFront;
    [Tooltip("앞면과 같은 위치에 배치한 뒷면 이미지 오브젝트")]
    [SerializeField] private GameObject cardBack;
    [Range(1f, 1.2f)] [SerializeField] private float hoverScale = 1.06f;
    [Min(0.01f)] [SerializeField] private float hoverDuration = 0.15f;
    private Vector3 originalScale;
    private bool scaleCached;
    private bool sold;
    private Tween scaleTween;
    private Action selected;
    public override bool IsConfigured => base.IsConfigured && button != null && priceText != null;

    //카드 원래 크기 저장
    private void Awake()
    {
        CacheScale();
    }

    //첫 표시 전 카드 원래 크기 저장
    private void CacheScale()
    {
        if (scaleCached)
            return;
        originalScale = transform.localScale;
        scaleCached = true;
    }

    //상품 정보와 구매 콜백 연결
    public void Show(CardData card, Action onSelected)
    {
        CacheScale();
        ResetScale();
        sold = false;
        RefreshCardFace();
        selected = onSelected;
        button.onClick.RemoveListener(Select);
        button.onClick.AddListener(Select);
        SetDisplay(card, card.Cost, CardDescriptionFormatter.FormatDescription(card));
        gameObject.SetActive(true);
    }

    //가격과 품절 및 구매 가능 상태 표시
    public void SetPurchaseState(int price, bool isSold, bool affordable)
    {
        sold = isSold;
        RefreshCardFace();
        priceText.text = sold ? "품절" : $"{price} 코인";
        button.interactable = !sold && affordable;
        if (sold)
            ResetScale();
    }

    //품절 시 뒷면 표시, 미연결 상태에서는 기존 앞면 유지
    private void RefreshCardFace()
    {
        if (cardFront == null || cardBack == null)
            return;

        cardFront.SetActive(!sold);
        cardBack.SetActive(sold);
    }

    //카드 클릭의 구매 요청 전달
    private void Select()
    {
        if (!sold && button.interactable)
            selected?.Invoke();
    }

    //포인터 진입 시 카드 확대
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!sold)
            AnimateScale(hoverScale);
    }

    //포인터 이탈 시 카드 원래 크기 복원
    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateScale(1f);
    }

    //기존 트윈 종료와 목표 크기 전환
    private void AnimateScale(float multiplier)
    {
        CacheScale();
        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalScale * multiplier, hoverDuration)
            .SetEase(Ease.OutQuad).SetUpdate(true);
    }

    //확대 트윈 종료와 기본 크기 즉시 복원
    private void ResetScale()
    {
        scaleTween?.Kill();
        scaleTween = null;
        if (scaleCached)
            transform.localScale = originalScale;
    }

    //상점 종료 시 카드 크기와 구매 입력 정리
    private void OnDisable()
    {
        ResetScale();
        if (button != null)
            button.onClick.RemoveListener(Select);
        selected = null;
    }
}
