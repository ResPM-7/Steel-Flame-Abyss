using System;

namespace SteelFlameAbyss.Battle
{
    /// <summary>
    /// 전투 중에만 사용하는 캐릭터/적의 가변 상태입니다.
    /// ScriptableObject 원본 데이터는 변경하지 않습니다.
    /// </summary>
    public sealed class CombatantState
    {
        /// <summary>체력, 방어도, 능력치가 변경된 직후 알립니다.</summary>
        public event Action Changed;

        public int MaxHealth { get; }
        public int CurrentHealth { get; private set; }
        public int Block { get; private set; }
        public int Strength { get; private set; }
        public int Weak { get; private set; }
        public int Vulnerable { get; private set; }

        public bool IsAlive => CurrentHealth > 0;
        public int MissingHealth => MaxHealth - CurrentHealth;

        public CombatantState(int maxHealth)
            : this(maxHealth, maxHealth)
        {
        }

        public CombatantState(int maxHealth, int currentHealth)
        {
            if (maxHealth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHealth), "최대 체력은 1 이상이어야 합니다.");

            MaxHealth = maxHealth;
            CurrentHealth = Math.Max(0, Math.Min(currentHealth, maxHealth));
        }

        /// <summary>
        /// 방어도로 먼저 피해를 막고, 실제로 감소한 체력을 반환합니다.
        /// </summary>
        public int TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return 0;

            int blockedDamage = Math.Min(Block, amount);
            Block -= blockedDamage;

            int healthDamage = Math.Min(CurrentHealth, amount - blockedDamage);
            CurrentHealth -= healthDamage;
            Changed?.Invoke();
            return healthDamage;
        }

        /// <summary>실제로 회복된 체력을 반환합니다.</summary>
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

        public void GainBlock(int amount)
        {
            if (amount > 0 && IsAlive)
            {
                Block += amount;
                Changed?.Invoke();
            }
        }

        public void ClearBlock()
        {
            if (Block == 0) return;
            Block = 0;
            Changed?.Invoke();
        }

        public void GainStrength(int amount)
        {
            if (amount > 0 && IsAlive)
            {
                Strength += amount;
                Changed?.Invoke();
            }
        }

        public void ApplyWeak(int amount)
        {
            if (amount > 0 && IsAlive)
            {
                Weak += amount;
                Changed?.Invoke();
            }
        }

        public void ApplyVulnerable(int amount)
        {
            if (amount > 0 && IsAlive)
            {
                Vulnerable += amount;
                Changed?.Invoke();
            }
        }

        /// <summary>턴 종료 시 약화와 취약 지속량을 1씩 감소시킵니다.</summary>
        public void TickDebuffs()
        {
            if (Weak == 0 && Vulnerable == 0) return;
            Weak = Math.Max(0, Weak - 1);
            Vulnerable = Math.Max(0, Vulnerable - 1);
            Changed?.Invoke();
        }
    }
}
