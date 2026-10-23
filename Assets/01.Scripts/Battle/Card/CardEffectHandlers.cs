//전투 대상이 필요한 효과의 공통 검증과 전달
public abstract class TargetedCardEffectHandler : ICardEffectHandler
{
    //명시적인 전투 대상 종류만 허용
    public bool CanApply(EffectSpec effect, CardEffectContext context) => effect.Target is
        TargetType.Self or TargetType.SingleEnemy or TargetType.AllEnemies or TargetType.RandomEnemy;

    //현재 대상 조회 후 효과 실행
    public void Apply(EffectSpec effect, CardEffectContext context)
    {
        CombatantState target = context.GetTarget(effect.Target);
        if (target != null) ApplyToTarget(effect, context, target);
    }

    //효과별 전투 상태 변경
    protected abstract void ApplyToTarget(EffectSpec effect, CardEffectContext context, CombatantState target);
}

//힘 보정 공격 피해와 피격 반격 연결
public class DamageEffectHandler : TargetedCardEffectHandler
{
    protected override void ApplyToTarget(EffectSpec effect, CardEffectContext context, CombatantState target) =>
        target.TakeAttackDamage(context.CalculateBaseDamage(effect), context.Participants.Player.Stats);
}

//방어도 획득
public class BlockEffectHandler : TargetedCardEffectHandler
{
    protected override void ApplyToTarget(EffectSpec effect, CardEffectContext context, CombatantState target) => target.GainBlock(effect.Amount);
}

//힘 획득
public class StrengthEffectHandler : TargetedCardEffectHandler
{
    protected override void ApplyToTarget(EffectSpec effect, CardEffectContext context, CombatantState target) => target.GainStrength(effect.Amount);
}

//약화 부여
public class WeakEffectHandler : TargetedCardEffectHandler
{
    protected override void ApplyToTarget(EffectSpec effect, CardEffectContext context, CombatantState target) => target.ApplyWeak(effect.Amount);
}

//취약 부여
public class VulnerableEffectHandler : TargetedCardEffectHandler
{
    protected override void ApplyToTarget(EffectSpec effect, CardEffectContext context, CombatantState target) => target.ApplyVulnerable(effect.Amount);
}

//화상 중첩 부여
public class BurnEffectHandler : TargetedCardEffectHandler
{
    protected override void ApplyToTarget(EffectSpec effect, CardEffectContext context, CombatantState target) => target.ApplyBurn(effect.Amount);
}

//다음 자기 턴까지 화상 반격 부여
public class BurnRetaliationEffectHandler : TargetedCardEffectHandler
{
    protected override void ApplyToTarget(EffectSpec effect, CardEffectContext context, CombatantState target) => target.ApplyBurnRetaliation(effect.Amount);
}

//플레이어 덱에서 추가 드로우
public class DrawEffectHandler : ICardEffectHandler
{
    public bool CanApply(EffectSpec effect, CardEffectContext context) => true;
    public void Apply(EffectSpec effect, CardEffectContext context) => context.Deck.DrawCards(effect.Amount);
}

//플레이어 에너지 획득
public class GainEnergyEffectHandler : ICardEffectHandler
{
    public bool CanApply(EffectSpec effect, CardEffectContext context) => true;
    public void Apply(EffectSpec effect, CardEffectContext context) => context.Participants.Player.GainEnergy(effect.Amount);
}

//카드 사용 완료 단계에서 처리할 소멸 표식
public class ExhaustEffectHandler : ICardEffectHandler
{
    public bool CanApply(EffectSpec effect, CardEffectContext context) => true;
    //실제 소멸은 CardPlayController의 FinishPlayingCard에서 한 번만 처리
    public void Apply(EffectSpec effect, CardEffectContext context) { }
}
