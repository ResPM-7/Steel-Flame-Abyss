using SteelFlameAbyss.Battle;
using TMPro;
using UnityEngine;

//슬롯에 연결된 카드의 이름, 비용, 설명을 표시합니다.
public class CardView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private CardHoverTween hoverTween;

    public CardInstance Card { get; private set; }
    public bool IsConfigured => nameText != null && costText != null &&
        descriptionText != null && hoverTween != null;

    //전달받은 런타임 카드 정보를 표시하고 슬롯과 호버 연결을 활성화합니다.
    public void Bind(CardInstance card, RectTransform hoverLayer)
    {
        Card = card;
        nameText.text = card.Data.DisplayName;
        costText.text = card.Cost.ToString();
        descriptionText.text = card.Description;
        gameObject.SetActive(true);
        hoverTween.Initialize((RectTransform)transform, hoverLayer);
        hoverTween.gameObject.SetActive(true);
    }

    //호버 중인 카드도 원래 슬롯으로 복귀시킨 다음 슬롯을 숨깁니다.
    public void Hide()
    {
        hoverTween.ReturnImmediately();
        hoverTween.gameObject.SetActive(false);
        Card = null;
        nameText.text = string.Empty;
        costText.text = string.Empty;
        descriptionText.text = string.Empty;
        gameObject.SetActive(false);
    }
}
