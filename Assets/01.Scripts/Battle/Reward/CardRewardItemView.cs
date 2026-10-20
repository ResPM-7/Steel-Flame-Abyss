using System;
using UnityEngine;

//보상 카드 정보와 희귀도 테두리 및 선택 입력 표시
public class CardRewardItemView : CardDisplayView
{
    [SerializeField] private UnityEngine.UI.Button button;
    private Action selected;
    public override bool IsConfigured => base.IsConfigured && button != null;

    //카드 표시와 선택 콜백 및 테두리 색상 연결
    public void Show(CardData card, Action onSelected)
    {
        selected = onSelected;
        button.onClick.RemoveListener(Select);
        button.onClick.AddListener(Select);
        SetDisplay(card, card.Cost, CardDescriptionFormatter.FormatDescription(card));
        gameObject.SetActive(true);
    }


    //카드 선택 입력 전달
    private void Select()
    {
        selected?.Invoke();
    }

    //카드 제거 시 버튼 구독 해제
    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(Select);
        selected = null;
    }
}
