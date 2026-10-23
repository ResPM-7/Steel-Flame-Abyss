using System;

//개체별로 체력과 다음 행동을 보관
public class EnemyBattleState
{
    public EnemyData Data { get; }
    public CombatantState Stats { get; }
    public EnemyActionSpec? PlannedAction { get; private set; }
    public event Action IntentChanged;
    private readonly Random random;

    //적의 체력을 설정 범위에서 뽑고 첫 행동을 선택
    public EnemyBattleState(EnemyData data, Random random)
    {
        Data = data != null ? data : throw new ArgumentNullException(nameof(data));
        if (random == null)
            throw new ArgumentNullException(nameof(random));

        this.random = random;
        if (data.MinHealth <= 0 || data.MaxHealth < data.MinHealth)
            throw new ArgumentException("적의 체력 범위가 올바르지 않습니다.", nameof(data));
        //최댓값 포함과 int 오버플로 방지
        var health = data.MinHealth + (int)(random.NextDouble() *
            ((long)data.MaxHealth - data.MinHealth + 1));
        Stats = new CombatantState(health);
        PlanNextAction();
    }

    //살아 있는 적의 다음 행동을 시트 가중치로 선택
    public void PlanNextAction()
    {
        PlannedAction = null;
        double total = 0;
        if (Stats.IsAlive)
            foreach (var action in Data.Actions)
                if (IsSelectable(action)) total += action.Weight;

        if (total > 0)
        {
            var roll = random.NextDouble() * total;
            foreach (var action in Data.Actions)
            {
                if (!IsSelectable(action)) continue;
                PlannedAction = action;
                roll -= action.Weight;
                if (roll < 0) break;
            }
        }
        IntentChanged?.Invoke();
    }

    //예고한 적 행동을 플레이어와 자신의 상태에 적용
    public void ExecutePlannedAction(CombatantState player)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));
        if (!Stats.IsAlive || !player.IsAlive || !PlannedAction.HasValue)
            return;

        EnemyActionSpec action = PlannedAction.Value;
        PlannedAction = null;
        foreach (EffectSpec effect in action.Effects)
        {
            if (!Stats.IsAlive || !player.IsAlive)
                break;

            CombatantState target;
            switch (effect.Target)
            {
                case TargetType.Self:
                    target = Stats;
                    break;
                case TargetType.SingleEnemy:
                case TargetType.AllEnemies:
                case TargetType.RandomEnemy:
                    target = player;
                    break;
                default:
                    GameLogger.LogWarning($"[적 행동] 지원하지 않는 대상 {effect.Target}");
                    continue;
            }

            switch (effect.Type)
            {
                case EffectType.Damage:
                    target.TakeAttackDamage(GetBaseDamage(effect, target), Stats);
                    break;
                case EffectType.Block:
                    target.GainBlock(effect.Amount);
                    break;
                case EffectType.Strength:
                    target.GainStrength(effect.Amount);
                    break;
                case EffectType.Weak:
                    target.ApplyWeak(effect.Amount);
                    break;
                case EffectType.Vulnerable:
                    target.ApplyVulnerable(effect.Amount);
                    break;
                case EffectType.Burn:
                    target.ApplyBurn(effect.Amount);
                    break;
                case EffectType.BurnRetaliation:
                    target.ApplyBurnRetaliation(effect.Amount);
                    break;
                default:
                    GameLogger.LogWarning($"[적 행동] 지원하지 않는 효과 {effect.Type}");
                    break;
            }
        }

        IntentChanged?.Invoke();
    }

    //상태 보정 전 적 공격 피해 계산
    private int GetBaseDamage(EffectSpec effect, CombatantState target) =>
        (int)Math.Max(0L, Math.Min(int.MaxValue, (long)effect.Amount + (target != Stats ? Stats.Strength : 0)));

    //실제 피격 계산과 동일한 적 행동 예고 수치
    public int GetPreviewAmount(EffectSpec effect, CombatantState player)
    {
        if (effect.Type != EffectType.Damage) return effect.Amount;
        CombatantState target = effect.Target == TargetType.Self ? Stats : player;
        return target.CalculateAttackDamage(GetBaseDamage(effect, target), Stats);
    }

    //효과가 있고 가중치가 유효한 선택 가능한 행동인지 확인
    private static bool IsSelectable(EnemyActionSpec action) =>
        action.Weight > 0 && !float.IsInfinity(action.Weight) &&
        action.Effects != null && action.Effects.Count > 0;
}
