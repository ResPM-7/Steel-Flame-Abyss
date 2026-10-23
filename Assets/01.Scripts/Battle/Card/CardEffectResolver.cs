using UnityEngine;

//카드 효과 데이터를 플레이어와 적의 전투 상태에 적용
public class CardEffectResolver
{
    private readonly IBattleDeckManager deckManager;
    private readonly BattleParticipants participants;

    //카드 효과 처리에 필요한 전투 객체 저장
    public CardEffectResolver(IBattleDeckManager manager, BattleParticipants battleParticipants)
    {
        if (manager == null)
            throw new System.ArgumentNullException(nameof(manager));
        if (battleParticipants == null)
            throw new System.ArgumentNullException(nameof(battleParticipants));

        deckManager = manager;
        participants = battleParticipants;
    }

    //카드의 모든 효과가 현재 구현된 효과인지 확인
    public bool CanResolve(CardInstance card)
    {
        if (card == null || card.Effects == null || card.Effects.Count == 0)
            return false;

        for (int index = 0; index < card.Effects.Count; index++)
        {
            if (!CanResolveEffect(card.Effects[index]))
                return false;
        }

        return true;
    }

    //카드 정보와 대상 규칙에 따른 효과 순차 적용
    public void Resolve(CardInstance card)
    {
        for (int index = 0; index < card.Effects.Count; index++)
        {
            if (!participants.IsBattleActive)
                break;
            ResolveEffect(card, card.Effects[index]);
        }
    }

    //현재 지원하는 효과 종류와 대상 조합 확인
    private static bool CanResolveEffect(EffectSpec effect)
    {
        switch (effect.Type)
        {
            case EffectType.Damage:
            case EffectType.Block:
            case EffectType.Strength:
            case EffectType.Weak:
            case EffectType.Vulnerable:
            case EffectType.Burn:
            case EffectType.BurnRetaliation:
                return effect.Target != TargetType.None;

            case EffectType.Draw:
            case EffectType.GainEnergy:
            case EffectType.Exhaust:
                return true;

            default:
                return false;
        }
    }

    //카드 종류와 효과에 따른 전투 상태 변경
    private void ResolveEffect(CardInstance card, EffectSpec effect)
    {
        CombatantState target = GetTargetState(effect.Target);

        switch (effect.Type)
        {
            case EffectType.Damage:
                target?.TakeAttackDamage(CalculateDamage(card, effect), participants.Player.Stats);
                break;
            case EffectType.Block:
                target?.GainBlock(effect.Amount);
                break;
            case EffectType.Strength:
                target?.GainStrength(effect.Amount);
                break;
            case EffectType.Draw:
                deckManager.DrawCards(effect.Amount);
                break;
            case EffectType.GainEnergy:
                participants.Player.GainEnergy(effect.Amount);
                break;
            case EffectType.Weak:
                target?.ApplyWeak(effect.Amount);
                break;
            case EffectType.Vulnerable:
                target?.ApplyVulnerable(effect.Amount);
                break;
            case EffectType.Burn:
                target?.ApplyBurn(effect.Amount);
                break;
            case EffectType.BurnRetaliation:
                target?.ApplyBurnRetaliation(effect.Amount);
                break;
        }
    }

    //적 대상 공격 카드의 기본 피해에 현재 힘 합산
    public int CalculateDamage(CardInstance card, EffectSpec effect)
    {
        bool targetsEnemy = effect.Target == TargetType.SingleEnemy ||
            effect.Target == TargetType.AllEnemies ||
            effect.Target == TargetType.RandomEnemy;

        int damage = effect.Amount;
        if (card.Data.CardType == CardType.Attack && targetsEnemy && participants.Player != null)
            damage += participants.Player.Stats.Strength;

        return Mathf.Max(0, damage);
    }

    //효과 대상 종류를 현재 전투 상태로 변환
    private CombatantState GetTargetState(TargetType targetType)
    {
        switch (targetType)
        {
            case TargetType.Self:
                return participants.Player.Stats;
            case TargetType.SingleEnemy:
            case TargetType.AllEnemies:
            case TargetType.RandomEnemy:
                return participants.Enemy.Stats;
            default:
                return null;
        }
    }
}
