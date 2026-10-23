using UnityEngine;

//한 카드 실행의 원본 정보와 전투 의존성 전달
public class CardEffectContext
{
    public CardInstance Card { get; }
    public IBattleDeckManager Deck { get; }
    public BattleParticipants Participants { get; }

    //실행 중 교체 없는 카드와 전투 참조 저장
    public CardEffectContext(CardInstance card, IBattleDeckManager deck, BattleParticipants participants)
    {
        Card = card;
        Deck = deck;
        Participants = participants;
    }

    //현재 1대1 전투의 대상 규칙 변환
    public CombatantState GetTarget(TargetType target) => target switch
    {
        TargetType.Self => Participants.Player?.Stats,
        TargetType.SingleEnemy or TargetType.AllEnemies or TargetType.RandomEnemy => Participants.Enemy?.Stats,
        _ => null
    };

    //실제 실행과 설명에서 공통 사용하는 힘 보정 피해
    public int CalculateDamage(EffectSpec effect)
    {
        bool enemy = effect.Target == TargetType.SingleEnemy ||
            effect.Target == TargetType.AllEnemies || effect.Target == TargetType.RandomEnemy;
        int damage = effect.Amount;
        if (Card.Data.CardType == CardType.Attack && enemy && Participants.Player != null)
            damage += Participants.Player.Stats.Strength;
        return Mathf.Max(0, damage);
    }
}
