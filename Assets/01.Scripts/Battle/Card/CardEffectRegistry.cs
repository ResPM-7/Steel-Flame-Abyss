using System;
using System.Collections.Generic;

//효과 종류별 처리기 등록과 조회
public class CardEffectRegistry
{
    private readonly Dictionary<EffectType, ICardEffectHandler> handlers = new();

    //빈 처리기와 동일 효과 중복 등록 차단
    public void Register(EffectType type, ICardEffectHandler handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        handlers.Add(type, handler);
    }

    //검증과 실행에서 동일 처리기 조회
    public bool TryGet(EffectType type, out ICardEffectHandler handler) => handlers.TryGetValue(type, out handler);

    //기본 카드 효과 구성과 확장 지점
    public static CardEffectRegistry CreateDefault()
    {
        var registry = new CardEffectRegistry();
        registry.Register(EffectType.Damage, new DamageEffectHandler());
        registry.Register(EffectType.LoseHealth, new LoseHealthEffectHandler());
        registry.Register(EffectType.Block, new BlockEffectHandler());
        registry.Register(EffectType.Strength, new StrengthEffectHandler());
        registry.Register(EffectType.Weak, new WeakEffectHandler());
        registry.Register(EffectType.Vulnerable, new VulnerableEffectHandler());
        registry.Register(EffectType.Burn, new BurnEffectHandler());
        registry.Register(EffectType.BurnRetaliation, new BurnRetaliationEffectHandler());
        registry.Register(EffectType.MindFracture, new MindFractureEffectHandler());
        registry.Register(EffectType.AmplifyMindFracture, new AmplifyMindFractureEffectHandler());
        registry.Register(EffectType.Draw, new DrawEffectHandler());
        registry.Register(EffectType.GainEnergy, new GainEnergyEffectHandler());
        registry.Register(EffectType.Exhaust, new ExhaustEffectHandler());
        return registry;
    }
}
