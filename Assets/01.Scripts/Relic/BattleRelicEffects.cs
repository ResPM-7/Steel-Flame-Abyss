using System;

//보유 유물의 시트 정의를 전투 시작과 플레이어 턴 시작에 적용
public sealed class BattleRelicEffects
{
    private readonly OwnedRelicModel owned;
    private readonly IGameDataProvider data;
    private readonly BattleParticipants participants;
    private readonly CardHandController hand;

    public BattleRelicEffects(OwnedRelicModel owned, IGameDataProvider data,
        BattleParticipants participants, CardHandController hand)
    {
        this.owned = owned ?? throw new ArgumentNullException(nameof(owned));
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.participants = participants ?? throw new ArgumentNullException(nameof(participants));
        this.hand = hand ?? throw new ArgumentNullException(nameof(hand));
    }

    //현재 구현 대상으로 지정한 세 유물만 발동하고 70003은 제외
    public void Apply(RelicTrigger trigger)
    {
        if (!participants.IsBattleActive || !hand.IsReady) return;
        foreach (int id in owned.RelicIds)
        {
            if (id != 70001 && id != 70002 && id != 70004) continue;
            if (!data.TryGetRelic(id, out RelicData relic) || relic.Trigger != trigger) continue;
            foreach (EffectSpec effect in relic.Effects)
            {
                if (effect.Target != TargetType.Self || effect.Amount <= 0) continue;
                switch (effect.Type)
                {
                    case EffectType.Block:
                        participants.Player.Stats.GainBlock(effect.Amount);
                        break;
                    case EffectType.Draw:
                        hand.DrawCards(effect.Amount);
                        break;
                    case EffectType.GainEnergy:
                        participants.Player.GainEnergy(effect.Amount);
                        break;
                }
            }
        }
    }
}
