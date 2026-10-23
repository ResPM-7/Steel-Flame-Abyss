using System.Text.RegularExpressions;

//보상과 목록 카드의 기본 설명 수치 변환
public static class CardDescriptionFormatter
{
    private static readonly Regex indexedTokens = new Regex(@"\{f(?<index>[1-9]\d*)\}");

    //효과 순번 토큰을 기본 또는 전투 보정 수치로 변환
    public static string FormatEffectSlots(string description,
        System.Collections.Generic.IReadOnlyList<EffectSpec> effects,
        System.Func<EffectSpec, int> getAmount = null)
    {
        return indexedTokens.Replace(description ?? string.Empty, match =>
        {
            if (!int.TryParse(match.Groups["index"].Value, out int slot) || slot > effects.Count)
                return match.Value;
            EffectSpec effect = effects[slot - 1];
            return (getAmount != null ? getAmount(effect) : effect.Amount).ToString();
        });
    }
    private static readonly Regex tokens = new Regex(@"\{(?<token>damage|shield|power|burn|mindFracture|draw)(?::(?<index>\d+))?\}|(?<![A-Za-z0-9_{])(?<token>damage|shield|power|burn|mindFracture|draw)(?![A-Za-z0-9_}])");

    //카드의 기본 수치 설명 생성
    public static string FormatDescription(CardData card)
    {
        string description = tokens.Replace(card.Description ?? string.Empty, match => GetValue(card, match));
        return FormatEffectSlots(description, card.Effects);
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

}
