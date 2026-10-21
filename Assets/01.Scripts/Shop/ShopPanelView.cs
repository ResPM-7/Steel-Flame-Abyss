using System;
using System.Collections.Generic;
using UnityEngine;

//상점 상품 화면과 맵 복귀 입력 전달
public class ShopPanelView : MonoBehaviour
{
    [SerializeField] private ShopCardItemView[] items;
    [SerializeField] private UnityEngine.UI.Button mapButton;
    [SerializeField] private int cardPrice = 50;
    [Header("유물 상품")]
    [SerializeField] private RectTransform relicRoot;
    [SerializeField] private ShopRelicItemView relicItemPrefab;
    [SerializeField] private RelicTooltipView relicTooltipPrefab;
    [SerializeField] private TMPro.TMP_Text noRelicsText;
    [Range(1, 3)] [SerializeField] private int relicOfferCount = 1;
    [SerializeField] private RelicShopPrices relicPrices = new RelicShopPrices();
    private readonly List<ShopRelicItemView> relicItems = new List<ShopRelicItemView>();
    private RelicTooltipView relicTooltip;
    private ShopRelicItemView hoveredRelic;
    public RelicShopPrices RelicPrices => relicPrices;
    public int RelicOfferCount => Mathf.Clamp(relicOfferCount, 1, 3);
    public event Action<int> RelicBuyRequested;
    public int CardPrice => Mathf.Max(1, cardPrice);
    public event Action<int> BuyRequested;
    public event Action LeaveRequested;
    public bool IsConfigured => mapButton != null && items != null && items.Length == 3 &&
        Array.TrueForAll(items, item => item != null && item.IsConfigured) &&
        new HashSet<ShopCardItemView>(items).Count == 3 && relicRoot != null &&
        relicItemPrefab != null && relicItemPrefab.IsConfigured && relicTooltipPrefab != null && noRelicsText != null;

    //상점 활성화와 상품별 구매 입력 연결
    public void Show(ShopModel model, int coins, OwnedRelicModel owned)
    {
        gameObject.SetActive(true);
        mapButton.onClick.RemoveListener(Leave);
        mapButton.onClick.AddListener(Leave);
        for (int i = 0; i < items.Length; i++)
        {
            bool available = i < model.Cards.Count;
            items[i].transform.parent.gameObject.SetActive(available);
            if (!available)
                continue;
            int index = i;
            items[i].Show(model.Cards[i], () => BuyRequested?.Invoke(index));
        }
        for (int i = 0; i < model.Relics.Count; i++)
        {
            if (i == relicItems.Count) relicItems.Add(Instantiate(relicItemPrefab, relicRoot, false));
            int index = i;
            relicItems[i].Show(model.Relics[i].Data, this, () => RelicBuyRequested?.Invoke(index));
        }
        for (int i = model.Relics.Count; i < relicItems.Count; i++) relicItems[i].gameObject.SetActive(false);
        noRelicsText.gameObject.SetActive(model.Relics.Count == 0);
        Refresh(model, coins, owned);
    }

    //보유 코인과 품절 상태를 상품에 반영
    public void Refresh(ShopModel model, int coins, OwnedRelicModel owned)
    {
        for (int i = 0; i < model.Cards.Count; i++)
            items[i].SetPurchaseState(model.Price, model.IsSold(i), coins >= model.Price);
        for (int i = 0; i < model.Relics.Count; i++)
            relicItems[i].Refresh(model.Relics[i], coins, owned.Contains(model.Relics[i].Data.Id));
    }

    //상점 공용 유물 설명창 최초 생성과 재사용
    public void ShowRelicTooltip(ShopRelicItemView source, RelicData relic, RectTransform target)
    {
        if (relicTooltip == null) relicTooltip = Instantiate(relicTooltipPrefab, transform, false);
        hoveredRelic = source;
        relicTooltip.Show(relic, target);
    }

    //현재 호버 상품의 설명 숨김
    public void HideRelicTooltip(ShopRelicItemView source = null)
    {
        if (source != null && source != hoveredRelic) return;
        hoveredRelic = null;
        if (relicTooltip != null) relicTooltip.Hide();
    }

    //맵 복귀 요청 전달
    private void Leave() => LeaveRequested?.Invoke();

    //상점 화면 숨김
    public void Close() => gameObject.SetActive(false);

    //상점 비활성화 시 복귀 버튼 구독 해제
    private void OnDisable()
    {
        HideRelicTooltip();
        if (mapButton != null)
            mapButton.onClick.RemoveListener(Leave);
    }
}
