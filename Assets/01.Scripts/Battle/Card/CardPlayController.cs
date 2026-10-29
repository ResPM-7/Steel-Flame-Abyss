using UnityEngine;
using System.Text.RegularExpressions;

//카드 대상과 에너지를 검사하고 카드 효과 실행
public class CardPlayController
{
    private readonly IBattleDeckManager deckManager;
    private readonly BattleParticipants participants;
    private readonly CardEffectResolver effectResolver;
    private static readonly Regex damageTextPattern = new Regex(@"(?<![\d.])\d+(?=\s*의?\s*피해)");
    private static readonly Regex effectTextPattern = new Regex(
        @"\{(?<token>damage|shield|power|burn|mindFracture|draw)(?::(?<index>\d+))?\}|(?<![A-Za-z0-9_{])(?<token>damage|shield|power|burn|mindFracture|draw)(?![A-Za-z0-9_}])");

    public event System.Action DescriptionsChanged
    {
        add { participants.DamagePreviewChanged += value; }
        remove { participants.DamagePreviewChanged -= value; }
    }

    //카드 설명의 효과 키워드를 현재 효과 수치로 변환
    public string GetDescription(CardInstance card, BattleCombatantView previewTarget = null)
    {
        if (card == null)
            return string.Empty;

        string description = card.Description ?? string.Empty;
        int damageCount = 0;
        EffectSpec damageEffect = default;
        for (int index = 0; index < card.Effects.Count; index++)
        {
            EffectSpec effect = card.Effects[index];
            if (effect.Type != EffectType.Damage)
                continue;

            damageEffect = effect;
            damageCount++;
        }

        if (damageCount == 1)
        {
            string damage = GetPreviewDamage(card, damageEffect, previewTarget).ToString();
            string baseDamage = damageEffect.Amount.ToString();
            description = damageTextPattern.Replace(description,
                match => match.Value == baseDamage ? damage : match.Value);
        }

        description = effectTextPattern.Replace(description, match => GetEffectText(card, match, previewTarget));
        return CardDescriptionFormatter.FormatEffectSlots(description, card.Effects,
            effect => effect.Type == EffectType.Damage
                ? GetPreviewDamage(card, effect, previewTarget) : effect.Amount);
    }

    //단일 적 카드의 드래그 대상에만 취약 보정 반영
    private int GetPreviewDamage(CardInstance card, EffectSpec effect, BattleCombatantView previewTarget)
    {
        var context = new CardEffectContext(card, deckManager, participants);
        if (effect.Target != TargetType.SingleEnemy)
            return context.CalculateDamage(effect);

        int damage = context.CalculateBaseDamage(effect);
        CombatantState attacker = participants.Player?.Stats;
        if (participants.TryGetEnemy(previewTarget, out var enemy))
            return enemy.Stats.CalculateAttackDamage(damage, attacker);

        return System.Math.Max(0, damage - (attacker?.Weak ?? 0));
    }

    //설명 키워드에 해당하는 효과 수치 반환
    private string GetEffectText(CardInstance card, Match match, BattleCombatantView previewTarget)
    {
        EffectType effectType;
        switch (match.Groups["token"].Value)
        {
            case "damage": effectType = EffectType.Damage; break;
            case "shield": effectType = EffectType.Block; break;
            case "power": effectType = EffectType.Strength; break;
            case "burn": effectType = EffectType.Burn; break;
            case "mindFracture": effectType = EffectType.MindFracture; break;
            case "draw": effectType = EffectType.Draw; break;
            default: return match.Value;
        }

        int effectIndex = 0;
        if (match.Groups["index"].Success &&
            !int.TryParse(match.Groups["index"].Value, out effectIndex))
            return match.Value;

        for (int index = 0; index < card.Effects.Count; index++)
        {
            EffectSpec effect = card.Effects[index];
            if (effect.Type != effectType)
                continue;
            if (effectIndex > 0)
            {
                effectIndex--;
                continue;
            }

            if (effectType == EffectType.Damage)
                return GetPreviewDamage(card, effect, previewTarget).ToString();

            return effect.Amount.ToString();
        }

        return match.Value;
    }

    //카드 사용에 필요한 덱과 전투 참가자 저장
    public CardPlayController(IBattleDeckManager manager, BattleParticipants battleParticipants)
    {
        if (manager == null)
            throw new System.ArgumentNullException(nameof(manager));
        if (battleParticipants == null)
            throw new System.ArgumentNullException(nameof(battleParticipants));

        deckManager = manager;
        participants = battleParticipants;
        effectResolver = new CardEffectResolver(manager, battleParticipants);
    }

    //카드 대상과 위치 및 에너지를 검사한 뒤 카드 사용
    public bool TryPlayCard(CardInstance card, BattleCombatantView targetView, bool raisedEnough)
    {
        if (SceneTransitionController.BlocksGameplay) return false;
        if (!participants.IsBattleActive ||
            !deckManager.ContainsInHand(card))
            return false;

        bool requiresEnemyTarget = RequiresEnemyTarget(card);
        bool validEnemy = participants.TryGetEnemy(targetView, out var selectedEnemy);
        if (requiresEnemyTarget && !validEnemy)
            return false;
        if (!requiresEnemyTarget && !raisedEnough)
            return false;
        if (!effectResolver.CanResolve(card))
        {
            GameLogger.LogWarning($"[카드 사용] {card.Data.DisplayName} 효과는 아직 지원하지 않습니다");
            return false;
        }
        if (participants.Player.Energy < card.Cost)
        {
            GameLogger.LogWarning($"[카드 사용] {card.Data.DisplayName} 카드의 에너지가 부족합니다");
            return false;
        }
        if (!participants.Player.TrySpendEnergy(card.Cost))
            return false;

        effectResolver.Resolve(card, selectedEnemy);
        bool exhaust = HasExhaustEffect(card);
        if (!deckManager.FinishPlayingCard(card, exhaust))
            return false;

        GameLogger.Log($"[카드 사용] {card.Data.DisplayName} 사용");
        return true;
    }

    //단일 적 선택 효과 포함 여부 확인
    private static bool RequiresEnemyTarget(CardInstance card)
    {
        for (int index = 0; index < card.Effects.Count; index++)
        {
            if (card.Effects[index].Target == TargetType.SingleEnemy)
                return true;
        }

        return false;
    }

    //사용 후 소멸 효과 포함 여부 확인
    private static bool HasExhaustEffect(CardInstance card)
    {
        for (int index = 0; index < card.Effects.Count; index++)
        {
            if (card.Effects[index].Type == EffectType.Exhaust)
                return true;
        }

        return false;
    }
}
