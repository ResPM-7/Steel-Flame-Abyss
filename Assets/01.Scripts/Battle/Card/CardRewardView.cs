using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

//카드 보상 선택창과 수령 버튼 표시
public class CardRewardView : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private UnityEngine.UI.Button openButton;
    [SerializeField] private UnityEngine.UI.Button closeButton;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private CardRewardItemView[] items = new CardRewardItemView[3];
    [Header("희귀도 테두리 색상")]
    [SerializeField] private Color starterColor = Color.gray;
    [SerializeField] private Color commonColor = Color.white;
    [SerializeField] private Color uncommonColor = Color.green;
    [SerializeField] private Color rareColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private Color specialColor = new Color(0.7f, 0.3f, 1f);

    public event Action OpenRequested;
    public event Action CloseRequested;
    public event Action<int> Selected;

    //필수 화면 연결과 카드 슬롯 중복 확인
    public bool ValidateReferences()
    {
        if (panel == null || openButton == null || closeButton == null || items == null || items.Length != 3)
            return false;
        var unique = new HashSet<CardRewardItemView>();
        foreach (CardRewardItemView item in items)
            if (item == null || !item.IsConfigured || !unique.Add(item))
                return false;
        return true;
    }

    //보상 버튼 입력 연결과 선택창 초기 숨김
    private void Awake()
    {
        if (!ValidateReferences())
        {
            Debug.LogError("[카드 보상] 패널과 버튼 및 카드 슬롯 세 개를 연결해 주세요", this);
            return;
        }
        openButton.onClick.AddListener(RequestOpen);
        closeButton.onClick.AddListener(RequestClose);
        Close();
        SetRewardState(false, false);
    }

    //보상 선택창 열기 요청 전달
    private void RequestOpen() { OpenRequested?.Invoke(); }

    //미수령 상태를 유지한 선택창 닫기 요청 전달
    private void RequestClose() { CloseRequested?.Invoke(); }

    //후보 카드와 희귀도 색상 표시 후 선택창 열기
    public void Show(IReadOnlyList<CardData> cards)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (i >= cards.Count)
            {
                items[i].gameObject.SetActive(false);
                continue;
            }
            int index = i;
            items[i].Show(cards[i], GetColor(cards[i].Rarity), () => Selected?.Invoke(index));
        }
        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
    }

    //선택창만 숨김
    public void Close() { panel.SetActive(false); }

    //카드 보상 버튼의 수령 상태 표시
    public void SetRewardState(bool available, bool claimed)
    {
        openButton.interactable = available && !claimed;
        if (rewardText != null)
            rewardText.text = claimed ? "수령 완료" : "Card";
    }

    //희귀도별 인스펙터 테두리 색상 반환
    private Color GetColor(DataRarity rarity)
    {
        switch (rarity)
        {
            case DataRarity.Starter: return starterColor;
            case DataRarity.Common: return commonColor;
            case DataRarity.Uncommon: return uncommonColor;
            case DataRarity.Rare: return rareColor;
            default: return specialColor;
        }
    }

    //화면 제거 시 버튼 입력 연결 해제
    private void OnDestroy()
    {
        if (openButton != null) openButton.onClick.RemoveListener(RequestOpen);
        if (closeButton != null) closeButton.onClick.RemoveListener(RequestClose);
    }
}
