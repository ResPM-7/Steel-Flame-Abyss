using System;
using System.Collections.Generic;

//보유 유물의 시트 정의와 전투 사건별 효과 적용
public class BattleRelicEffects
{
    private readonly OwnedRelicModel owned;
    private bool cardHealthLossTriggered;
    private int nextCardDiscount;
    public bool UsesHealthForNextCard { get; private set; }
    public event Action CostsChanged;
    public int NextCardDiscount => nextCardDiscount;
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
        registry.Register(EffectType.NextCardHealthCost, TargetType.Self, _ =>
        {
            UsesHealthForNextCard = true;
            CostsChanged?.Invoke();
        });
        registry.Register(EffectType.NextCardDiscount, TargetType.Self, AddNextCardDiscount);
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
        if (trigger == RelicTrigger.BattleStart || trigger == RelicTrigger.TurnStart)
        {
            ClearHealthPayment();
            cardHealthLossTriggered = false;
            ClearDiscount();
        }
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

    //살아 있는 플레이어의 첫 카드 체력 소모에만 발동
    public void OnCardHealthLost(int lost)
    {
        if (lost <= 0 || cardHealthLossTriggered || !participants.IsBattleActive || !hand.IsReady) return;
        cardHealthLossTriggered = true;
        Apply(RelicTrigger.FirstCardHealthLost);
    }

    //할인 중첩 시 정수 범위 초과 방지
    private void AddNextCardDiscount(int amount)
    {
        nextCardDiscount = (int)Math.Min(int.MaxValue, (long)nextCardDiscount + amount);
        CostsChanged?.Invoke();
    }

    //미리보기와 결제에서 동일한 비용 사용
    public int GetCardCost(CardInstance card) => Math.Max(0, card.Cost - nextCardDiscount);

    //성공한 카드 결제 직후 기존 할인 소모, 새 효과 발동 전 처리
    public void ConsumeCardDiscount()
    {
        ClearHealthPayment();
        ClearDiscount();
    }

    //적 행동 전에 사용하지 않은 할인 제거
    public void EndPlayerTurn()
    {
        ClearHealthPayment();
        ClearDiscount();
    }

    //첫 카드 결제 또는 턴 종료 시 체력 결제 예약 제거
    private void ClearHealthPayment()
    {
        if (!UsesHealthForNextCard) return;
        UsesHealthForNextCard = false;
        CostsChanged?.Invoke();
    }

    //보유 할인 제거 후 손패 비용 갱신
    private void ClearDiscount()
    {
        if (nextCardDiscount == 0) return;
        nextCardDiscount = 0;
        CostsChanged?.Invoke();
    }
}
