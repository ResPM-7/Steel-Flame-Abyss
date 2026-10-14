using System;

//캠프 방문별 회복량과 휴식 완료 상태 관리
public class CampVisitModel
{
    private readonly CombatantState health;
    public bool HasRested { get; private set; }
    public int HealAmount { get; }
    public int CurrentHealth => health.CurrentHealth;
    public int MaxHealth => health.MaxHealth;

    //플레이어 체력과 최대 체력 비례 회복량 저장
    public CampVisitModel(CombatantState health, int healPercent)
    {
        if (health == null)
            throw new ArgumentNullException(nameof(health));
        if (healPercent < 1 || healPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(healPercent));
        this.health = health;
        HealAmount = (int)(((long)health.MaxHealth * healPercent + 99) / 100);
    }

    //캠프당 한 번 회복과 실제 회복량 반환
    public bool TryRest(out int healed)
    {
        healed = 0;
        if (HasRested || !health.IsAlive)
            return false;
        HasRested = true;
        healed = health.Heal(HealAmount);
        return true;
    }
}
