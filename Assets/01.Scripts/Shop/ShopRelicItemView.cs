using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

//유물 자체 클릭과 가격 및 호버 설명 표시
public class ShopRelicItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UnityEngine.UI.Button button;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    private Action selected;
    private RelicData relic;
    private ShopPanelView owner;
    public bool IsConfigured => button != null && nameText != null && priceText != null;

    //유물 상품과 구매 입력 연결
    public void Show(RelicData data, ShopPanelView panel, Action onSelected)
    {
        relic = data;
        owner = panel;
        selected = onSelected;
        nameText.text = data.DisplayName;
        button.onClick.RemoveListener(Select);
        button.onClick.AddListener(Select);
        gameObject.SetActive(true);
    }

    //가격과 품절 및 중복 보유 상태 반영
    public void Refresh(ShopModel.RelicOffer offer, int coins, bool owned)
    {
        priceText.text = offer.Sold ? "품절" : owned ? "보유 중" : $"{offer.Price} 코인";
        button.interactable = !offer.Sold && !owned && offer.Data.EffectsEnabled && coins >= offer.Price;
    }

    //유물 클릭의 구매 요청 전달
    private void Select() { if (button.interactable) selected?.Invoke(); }

    //유물 호버 설명 표시
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (relic != null) owner.ShowRelicTooltip(this, relic, (RectTransform)button.transform);
    }

    //포인터 이탈 시 설명 숨김
    public void OnPointerExit(PointerEventData eventData) => owner?.HideRelicTooltip(this);

    //상점 숨김 시 입력과 설명 연결 해제
    private void OnDisable()
    {
        owner?.HideRelicTooltip(this);
        if (button != null) button.onClick.RemoveListener(Select);
        selected = null;
    }
}
