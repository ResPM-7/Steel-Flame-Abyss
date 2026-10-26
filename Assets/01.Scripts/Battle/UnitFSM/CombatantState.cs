using System;

//전투 중에만 사용하는 캐릭터/적의 가변 상태
//ScriptableObject 원본 데이터는 변경하지 않음
public class CombatantState
{
    //체력 방어도 능력치가 변경된 직후 알림
    public event Action Changed;
    //공격과 실제 피해 발생 시 공통 연출 알림
    public event Action AttackPerformed;
    public event Action Damaged;

    public int MaxHealth { get; }
    public int CurrentHealth { get; private set; }
    public int Block { get; private set; }
    public int Strength { get; private set; }
    public int Weak { get; private set; }
    public int Vulnerable { get; private set; }
    public int Burn { get; private set; }
    public int MindFracture { get; private set; }
    public bool CanReceiveMindFracture { get; }
    private readonly BurnRetaliationEffect burnRetaliation = new BurnRetaliationEffect();
    public int BurnRetaliation => burnRetaliation.Amount;

    //살아 있는 대상에게 화상 반격 부여
    public void ApplyBurnRetaliation(int amount)
    {
        if (amount <= 0 || !IsAlive) return;
        burnRetaliation.Add(amount);
        Changed?.Invoke();
    }

    //자기 턴 시작 시 반격 상태 초기화
    public void ClearBurnRetaliation()
    {
        if (BurnRetaliation == 0) return;
        burnRetaliation.Clear();
        Changed?.Invoke();
    }

    //직접 공격 피해 처리 후 생존 시 반격 실행
    public int TakeAttackDamage(int amount, CombatantState attacker)
    {
        if (amount <= 0 || !IsAlive) return 0;
        attacker?.AttackPerformed?.Invoke();
        int lost = TakeDamage(CalculateAttackDamage(amount, attacker));
        burnRetaliation.OnAttacked(this, attacker);
        return lost;
    }

    //힘 보정 이후 약화 차감과 취약 배율 적용, 소수점 버림
    public int CalculateAttackDamage(int amount, CombatantState attacker)
    {
        long damage = Math.Max(0L, (long)amount - (attacker?.Weak ?? 0));
        if (Vulnerable > 0) damage = damage * 3 / 2;
        return (int)Math.Min(int.MaxValue, damage);
    }

    public bool IsAlive => CurrentHealth > 0;
    public int MissingHealth => MaxHealth - CurrentHealth;

    //최대 체력이 가득 찬 전투 상태를 생성
    public CombatantState(int maxHealth)
        : this(maxHealth, maxHealth)
    {
    }

    //최대 체력과 현재 체력으로 전투 상태를 생성
    public CombatantState(int maxHealth, int currentHealth, bool canReceiveMindFracture = false)
    {
        if (maxHealth <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxHealth), "최대 체력은 1 이상이어야 합니다.");

        MaxHealth = maxHealth;
        CanReceiveMindFracture = canReceiveMindFracture;
        CurrentHealth = Math.Max(0, Math.Min(currentHealth, maxHealth));
    }

    //방어도로 먼저 피해를 막고 실제로 감소한 체력을 반환
    public int TakeDamage(int amount)
    {
        if (amount <= 0 || !IsAlive)
            return 0;

        int blockedDamage = Math.Min(Block, amount);
        Block -= blockedDamage;

        int healthDamage = Math.Min(CurrentHealth, amount - blockedDamage);
        CurrentHealth -= healthDamage;
        Changed?.Invoke();
        Damaged?.Invoke();
        return healthDamage;
    }

    //방어도를 무시한 체력 손실 적용
    public int LoseHealth(int amount)
    {
        if (amount <= 0 || !IsAlive) return 0;
        int lost = Math.Min(CurrentHealth, amount);
        CurrentHealth -= lost;
        Changed?.Invoke();
        Damaged?.Invoke();
        return lost;
    }

    //적 전용 정신분열 누적 후 즉시 처형 검사
    public void ApplyMindFracture(int amount)
    {
        if (!CanReceiveMindFracture || !IsAlive || amount <= 0) return;
        MindFracture = (int)Math.Min(int.MaxValue, (long)MindFracture + amount);
        if (!TryExecuteMindFracture()) Changed?.Invoke();
    }

    //현재 정신분열 배수 증가 후 즉시 처형 검사
    public void AmplifyMindFracture(int multiplier)
    {
        if (!CanReceiveMindFracture || !IsAlive || MindFracture <= 0 || multiplier <= 1) return;
        MindFracture = (int)Math.Min(int.MaxValue, (long)MindFracture * multiplier);
        if (!TryExecuteMindFracture()) Changed?.Invoke();
    }

    //현재 체력 이상이면 방어도와 반격을 무시한 처형 및 승패 알림
    public bool TryExecuteMindFracture()
    {
        if (!CanReceiveMindFracture || !IsAlive || MindFracture <= 0 || MindFracture < CurrentHealth)
            return false;
        CurrentHealth = 0;
        Changed?.Invoke();
        return true;
    }

    //살아 있는 대상에게 화상 중첩 추가
    public void ApplyBurn(int amount)
    {
        if (amount <= 0 || !IsAlive) return;
        Burn = (int)Math.Min(int.MaxValue, (long)Burn + amount);
        Changed?.Invoke();
    }

    //대상 턴 종료 시 방어 무시 피해와 화상 1 감소 후 상태 알림
    public int ResolveBurnAtTurnEnd()
    {
        if (Burn <= 0 || !IsAlive) return 0;
        int lost = Math.Min(CurrentHealth, Burn);
        CurrentHealth -= lost;
        Burn--;
        Changed?.Invoke();
        Damaged?.Invoke();
        return lost;
    }

    //실제로 회복된 체력을 반환
    public int Heal(int amount)
    {
        if (amount <= 0 || !IsAlive)
            return 0;

        int healedAmount = Math.Min(MissingHealth, amount);
        CurrentHealth += healedAmount;
        if (healedAmount > 0)
            Changed?.Invoke();
        return healedAmount;
    }

    //살아 있는 대상의 방어도를 증가
    public void GainBlock(int amount)
    {
        if (amount > 0 && IsAlive)
        {
            Block += amount;
            Changed?.Invoke();
        }
    }

    //남아 있는 방어도를 초기화
    public void ClearBlock()
    {
        if (Block == 0) return;
        Block = 0;
        Changed?.Invoke();
    }

    //살아 있는 대상의 힘을 증가
    public void GainStrength(int amount)
    {
        if (amount > 0 && IsAlive)
        {
            Strength += amount;
            Changed?.Invoke();
        }
    }

    //살아 있는 대상에게 이번 턴 공격 감소량 누적
    public void ApplyWeak(int amount)
    {
        if (amount > 0 && IsAlive)
        {
            Weak = (int)Math.Min(int.MaxValue, (long)Weak + amount);
            Changed?.Invoke();
        }
    }

    //살아 있는 대상에게 취약 지속량을 추가
    public void ApplyVulnerable(int amount)
    {
        if (amount > 0 && IsAlive)
        {
            Vulnerable = (int)Math.Min(int.MaxValue, (long)Vulnerable + amount);
            Changed?.Invoke();
        }
    }

    //자신의 턴 종료 시 약화 전체 해제와 취약 남은 턴 1 감소
    public void TickDebuffs()
    {
        if (Weak == 0 && Vulnerable == 0) return;
        Weak = 0;
        Vulnerable = Math.Max(0, Vulnerable - 1);
        Changed?.Invoke();
    }
}
