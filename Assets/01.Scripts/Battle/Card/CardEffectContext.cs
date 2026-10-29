using UnityEngine;
using System.Collections.Generic;

//한 카드 실행의 원본 정보와 전투 의존성 전달
public class CardEffectContext
{
    private readonly EnemyBattleState selectedEnemy;
    public CardInstance Card { get; }
    public IBattleDeckManager Deck { get; }
    public BattleParticipants Participants { get; }

    //실행 중 교체 없는 카드와 전투 참조 저장
    public CardEffectContext(CardInstance card, IBattleDeckManager deck, BattleParticipants participants, EnemyBattleState target = null)
    {
        Card = card;
        Deck = deck;
        Participants = participants;
        selectedEnemy = target ?? participants.FirstAliveEnemy;
    }

    //단일 대상은 카드 사용 시 선택한 적에 고정
    public CombatantState GetTarget(TargetType target) => target switch
    {
        TargetType.Self => Participants.Player?.Stats,
        TargetType.SingleEnemy => selectedEnemy?.Stats,
        _ => null
    };

    //전체 적은 생존 개체만, 무작위 적은 효과마다 한 개체 선택
    public IEnumerable<CombatantState> GetTargets(TargetType target)
    {
        if (target == TargetType.AllEnemies)
        {
            foreach (var enemy in Participants.Enemies)
                if (enemy.Stats.IsAlive) yield return enemy.Stats;
        }
        else if (target == TargetType.RandomEnemy)
        {
            var enemy = Participants.GetRandomAliveEnemy();
            if (enemy != null) yield return enemy.Stats;
        }
        else
        {
            var state = GetTarget(target);
            if (state != null && state.IsAlive) yield return state;
        }
    }

    //약화와 취약 적용 전 힘 보정 피해
    public int CalculateBaseDamage(EffectSpec effect)
    {
        bool enemy = effect.Target == TargetType.SingleEnemy ||
            effect.Target == TargetType.AllEnemies || effect.Target == TargetType.RandomEnemy;
        long damage = effect.Amount;
        if (Card.Data.CardType == CardType.Attack && enemy && Participants.Player != null)
            damage += Participants.Player.Stats.Strength;
        return (int)System.Math.Max(0L, System.Math.Min(int.MaxValue, damage));
    }

    //실제 피격 계산과 동일한 카드 설명용 피해
    public int CalculateDamage(EffectSpec effect)
    {
        int damage = CalculateBaseDamage(effect);
        return GetTarget(effect.Target)?.CalculateAttackDamage(damage, Participants.Player?.Stats) ??
            System.Math.Max(0, damage - (Participants.Player?.Stats.Weak ?? 0));
    }
}
