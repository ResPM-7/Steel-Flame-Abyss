using System;

//등록된 처리기로 카드 효과 검증과 순차 실행
public class CardEffectResolver
{
    private readonly IBattleDeckManager deckManager;
    private readonly BattleParticipants participants;
    private readonly CardEffectRegistry registry;

    //전투 의존성과 기본 또는 사용자 정의 처리기 구성 주입
    public CardEffectResolver(IBattleDeckManager manager, BattleParticipants battleParticipants,
        CardEffectRegistry effectRegistry = null)
    {
        deckManager = manager ?? throw new ArgumentNullException(nameof(manager));
        participants = battleParticipants ?? throw new ArgumentNullException(nameof(battleParticipants));
        registry = effectRegistry ?? CardEffectRegistry.CreateDefault();
    }

    //에너지 소비 전 카드 전체 효과 지원 여부 확인
    public bool CanResolve(CardInstance card)
    {
        if (card == null || card.Effects == null || card.Effects.Count == 0) return false;
        return CanResolve(new CardEffectContext(card, deckManager, participants));
    }

    //실행에도 사용할 동일 처리기로 전체 효과 검증
    private bool CanResolve(CardEffectContext context)
    {
        foreach (EffectSpec effect in context.Card.Effects)
            if (!registry.TryGet(effect.Type, out var handler) || !handler.CanApply(effect, context))
                return false;
        return true;
    }

    //미지원 효과의 부분 실행 차단과 전투 종료 시 중단
    public void Resolve(CardInstance card, EnemyBattleState target = null)
    {
        if (card == null || card.Effects == null || card.Effects.Count == 0) return;
        var context = new CardEffectContext(card, deckManager, participants, target);
        if (!CanResolve(context)) return;
        foreach (EffectSpec effect in card.Effects)
        {
            if (!participants.IsBattleActive) break;
            if (registry.TryGet(effect.Type, out var handler)) handler.Apply(effect, context);
        }
    }

    //설명 미리보기와 실제 공격의 공통 피해 계산
    public int CalculateDamage(CardInstance card, EffectSpec effect) =>
        new CardEffectContext(card, deckManager, participants).CalculateDamage(effect);
}
