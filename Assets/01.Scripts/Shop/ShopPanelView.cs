using System;
using System.Collections.Generic;
using UnityEngine;

//상점 상품 화면과 맵 복귀 입력 전달
public class ShopPanelView : MonoBehaviour
{
    [SerializeField] private ShopCardItemView[] items;
    [SerializeField] private UnityEngine.UI.Button mapButton;
    [SerializeField] private int cardPrice = 50;
    public int CardPrice => Mathf.Max(1, cardPrice);
    public event Action<int> BuyRequested;
    public event Action LeaveRequested;
    public bool IsConfigured => mapButton != null && items != null && items.Length == 3 &&
        Array.TrueForAll(items, item => item != null && item.IsConfigured) &&
        new HashSet<ShopCardItemView>(items).Count == 3;

    //상점 활성화와 상품별 구매 입력 연결
    public void Show(ShopModel model, int coins)
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
        Refresh(model, coins);
    }

    //보유 코인과 품절 상태를 상품에 반영
    public void Refresh(ShopModel model, int coins)
    {
        for (int i = 0; i < model.Cards.Count; i++)
            items[i].SetPurchaseState(model.Price, model.IsSold(i), coins >= model.Price);
    }

    //맵 복귀 요청 전달
    private void Leave() => LeaveRequested?.Invoke();

    //상점 화면 숨김
    public void Close() => gameObject.SetActive(false);

    //상점 비활성화 시 복귀 버튼 구독 해제
    private void OnDisable()
    {
        if (mapButton != null)
            mapButton.onClick.RemoveListener(Leave);
    }
}
