using System;

//다음 자기 턴까지 유지되는 공격 피격 시 화상 반격
public class BurnRetaliationEffect
{
    public int Amount { get; private set; }

    //여러 번 부여한 반격 수치 합산
    public void Add(int amount)
    {
        if (amount <= 0) return;
        Amount = (int)Math.Min(int.MaxValue, (long)Amount + amount);
    }

    //방어 성공 여부와 무관한 생존 공격자 화상 부여
    public void OnAttacked(CombatantState defender, CombatantState attacker)
    {
        if (Amount <= 0 || defender == null || !defender.IsAlive ||
            attacker == null || attacker == defender || !attacker.IsAlive) return;
        attacker.ApplyBurn(Amount);
    }

    //다음 자기 턴 시작 시 반격 제거
    public void Clear() => Amount = 0;
}
