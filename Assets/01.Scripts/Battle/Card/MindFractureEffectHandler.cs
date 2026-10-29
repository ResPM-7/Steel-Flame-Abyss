//플레이어 카드의 적 대상 정신분열 효과 검증과 적용
public class MindFractureEffectHandler : ICardEffectHandler
{
    //자신 대상 차단 및 적 전용 상태 확인
    public bool CanApply(EffectSpec effect, CardEffectContext context) =>
        (effect.Target is TargetType.SingleEnemy or TargetType.AllEnemies or TargetType.RandomEnemy) &&
        context.Participants.Player != null;

    //플레이어 카드 경로에서만 정신분열 부여
    public virtual void Apply(EffectSpec effect, CardEffectContext context)
    {
        if (!CanApply(effect, context) || !context.Participants.IsBattleActive) return;
        foreach (var target in context.GetTargets(effect.Target))
        {
            if (!context.Participants.IsBattleActive) break;
            if (target.CanReceiveMindFracture) target.ApplyMindFracture(effect.Amount);
        }
    }
}

//현재 정신분열을 카드 수치 배수로 증가
public class AmplifyMindFractureEffectHandler : MindFractureEffectHandler
{
    //부여와 동일한 대상 제한으로 배수 증가
    public override void Apply(EffectSpec effect, CardEffectContext context)
    {
        if (!CanApply(effect, context) || !context.Participants.IsBattleActive) return;
        foreach (var target in context.GetTargets(effect.Target))
        {
            if (!context.Participants.IsBattleActive) break;
            if (target.CanReceiveMindFracture) target.AmplifyMindFracture(effect.Amount);
        }
    }
}
