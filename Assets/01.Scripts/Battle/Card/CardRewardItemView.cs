using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

//보상 카드 정보와 희귀도 테두리 및 선택 입력 표시
public class CardRewardItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private UnityEngine.UI.Button button;
    [SerializeField] private UnityEngine.UI.Outline outline;
    private Action selected;
    private static readonly Regex tokens = new Regex(@"\{(?<token>damage|shield|power|burn|mindFracture|draw)(?::(?<index>\d+))?\}|(?<![A-Za-z0-9_{])(?<token>damage|shield|power|burn|mindFracture|draw)(?![A-Za-z0-9_}])");
    public bool IsConfigured => nameText != null && costText != null && descriptionText != null && button != null && outline != null;

    //카드 표시와 선택 콜백 및 테두리 색상 연결
    public void Show(CardData card, Color color, Action onSelected)
    {
        selected = onSelected;
        button.onClick.RemoveListener(Select);
        button.onClick.AddListener(Select);
        nameText.text = card.DisplayName;
        costText.text = card.Cost.ToString();
        descriptionText.text = tokens.Replace(card.Description ?? string.Empty, match => GetValue(card, match));
        outline.enabled = true;
        outline.effectColor = color;
        gameObject.SetActive(true);
    }

    //설명 키워드를 기본 효과 수치로 변환
    private static string GetValue(CardData card, Match match)
    {
        EffectType type;
        switch (match.Groups["token"].Value)
        {
            case "damage": type = EffectType.Damage; break;
            case "shield": type = EffectType.Block; break;
            case "power": type = EffectType.Strength; break;
            case "burn": type = EffectType.Burn; break;
            case "mindFracture": type = EffectType.MindFracture; break;
            case "draw": type = EffectType.Draw; break;
            default: return match.Value;
        }
        int index = 0;
        if (match.Groups["index"].Success && !int.TryParse(match.Groups["index"].Value, out index))
            return match.Value;
        foreach (EffectSpec effect in card.Effects)
        {
            if (effect.Type == type && index-- == 0)
                return effect.Amount.ToString();
        }
        return match.Value;
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
