using System;
using System.Collections.Generic;

//효과 종류와 대상별 실행 기능 등록
public class RelicEffectRegistry
{
    private readonly Dictionary<(EffectType, TargetType), Action<int>> handlers =
        new Dictionary<(EffectType, TargetType), Action<int>>();

    //동일 효과와 대상의 중복 등록 차단
    public void Register(EffectType type, TargetType target, Action<int> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        handlers.Add((type, target), handler);
    }

    //등록된 효과의 수치 전달과 실행
    public bool TryApply(EffectSpec effect)
    {
        if (!handlers.TryGetValue((effect.Type, effect.Target), out Action<int> handler)) return false;
        if (effect.Amount > 0) handler(effect.Amount);
        return true;
    }
}
