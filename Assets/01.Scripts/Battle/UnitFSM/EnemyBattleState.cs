using System;

//개체별 체력과 다음 행동을 보관합니다. 적 SO는 수정하지 않습니다.
public class EnemyBattleState
{
    public EnemyData Data { get; }
    public CombatantState Stats { get; }
    public EnemyActionSpec? PlannedAction { get; private set; }
    public event Action IntentChanged;
    private readonly Random random;

    //적의 체력을 설정 범위에서 뽑고 첫 행동을 선택합니다.
    public EnemyBattleState(EnemyData data, Random random)
    {
        Data = data != null ? data : throw new ArgumentNullException(nameof(data));
        this.random = random ?? throw new ArgumentNullException(nameof(random));
        if (data.MinHealth <= 0 || data.MaxHealth < data.MinHealth)
            throw new ArgumentException("적의 체력 범위가 올바르지 않습니다.", nameof(data));
        // 최댓값도 포함하며 int.MaxValue + 1 오버플로를 피합니다.
        var health = data.MinHealth + (int)(random.NextDouble() *
            ((long)data.MaxHealth - data.MinHealth + 1));
        Stats = new CombatantState(health);
        PlanNextAction();
    }

    //살아 있는 적의 행동을 시트 가중치로 선택합니다. 실행은 전투 루프가 담당합니다.
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

    //효과가 있고 가중치가 유효한 선택 가능한 행동인지 확인합니다.
    private static bool IsSelectable(EnemyActionSpec action) =>
        action.Weight > 0 && !float.IsInfinity(action.Weight) &&
        action.Effects != null && action.Effects.Count > 0;
}
