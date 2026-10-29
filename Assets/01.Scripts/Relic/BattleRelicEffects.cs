using System;
using System.Collections.Generic;

//보유 유물의 시트 정의를 전투 시작과 플레이어 턴 시작에 적용
public class BattleRelicEffects
{
    private readonly OwnedRelicModel owned;
    private readonly IGameDataProvider data;
    private readonly BattleParticipants participants;
    private readonly CardHandController hand;
    private readonly RelicEffectRegistry registry = new RelicEffectRegistry();
    private readonly HashSet<(EffectType, TargetType)> unsupported = new HashSet<(EffectType, TargetType)>();

    public BattleRelicEffects(OwnedRelicModel owned, IGameDataProvider data,
        BattleParticipants participants, CardHandController hand)
    {
        this.owned = owned ?? throw new ArgumentNullException(nameof(owned));
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.participants = participants ?? throw new ArgumentNullException(nameof(participants));
        this.hand = hand ?? throw new ArgumentNullException(nameof(hand));
        registry.Register(EffectType.Block, TargetType.Self, amount => participants.Player.Stats.GainBlock(amount));
        registry.Register(EffectType.Draw, TargetType.Self, amount => hand.DrawCards(amount));
        registry.Register(EffectType.GainEnergy, TargetType.Self, amount => participants.Player.GainEnergy(amount));
        //카드와 같은 전투 상태 함수로 화상 부여
        registry.Register(EffectType.Burn, TargetType.Self, amount => participants.Player.Stats.ApplyBurn(amount));
        registry.Register(EffectType.Burn, TargetType.SingleEnemy, amount => participants.GetRandomAliveEnemy()?.Stats.ApplyBurn(amount));
        registry.Register(EffectType.Burn, TargetType.AllEnemies, amount =>
        {
            foreach (var enemy in participants.Enemies)
                if (enemy.Stats.IsAlive) enemy.Stats.ApplyBurn(amount);
        });
        registry.Register(EffectType.Burn, TargetType.RandomEnemy, amount => participants.GetRandomAliveEnemy()?.Stats.ApplyBurn(amount));
    }

    //보유 유물의 활성화 여부와 발동 시점으로 효과 실행
    public void Apply(RelicTrigger trigger)
    {
        if (!participants.IsBattleActive || !hand.IsReady) return;
        foreach (int id in owned.RelicIds)
        {
            if (!data.TryGetRelic(id, out RelicData relic) || !relic.EffectsEnabled || relic.Trigger != trigger) continue;
            foreach (EffectSpec effect in relic.Effects)
            {
                if (!registry.TryApply(effect) && unsupported.Add((effect.Type, effect.Target)))
                    GameLogger.LogWarning($"[유물] 미지원 효과 {effect.Type} / 대상 {effect.Target} / 유물 {id}");
            }
        }
    }
}
