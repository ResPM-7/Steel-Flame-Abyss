//카드 효과별 지원 조건과 실행 계약
public interface ICardEffectHandler
{
    bool CanApply(EffectSpec effect, CardEffectContext context);
    void Apply(EffectSpec effect, CardEffectContext context);
}
