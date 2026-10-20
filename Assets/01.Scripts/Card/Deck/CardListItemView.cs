using UnityEngine;

//호버와 드래그 없이 목록 카드 정보 표시
public class CardListItemView : CardDisplayView
{
    //목록 카드의 기본 정보와 희귀도 테두리 표시
    public void Show(CardData card)
    {
        if (card == null)
            throw new System.ArgumentNullException(nameof(card));
        SetDisplay(card, card.Cost, CardDescriptionFormatter.FormatDescription(card));
        gameObject.SetActive(true);
    }
}
