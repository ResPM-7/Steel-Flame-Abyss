using System;
using SteelFlameAbyss.Data;

namespace SteelFlameAbyss.Battle
{
    /// <summary>캐릭터 SO와 별도로 보관하는 플레이어의 전투 상태입니다.</summary>
    public sealed class PlayerBattleState
    {
        public CharacterData Data { get; }
        public CombatantState Stats { get; }
        public int Energy { get; private set; }
        public event Action Changed;

        public PlayerBattleState(CharacterData data)
        {
            Data = data != null ? data : throw new ArgumentNullException(nameof(data));
            Stats = new CombatantState(data.MaxHealth);
            Energy = Math.Max(0, data.StartingEnergy);
        }

        public bool TrySpendEnergy(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!Stats.IsAlive || amount > Energy) return false;
            if (amount == 0) return true;
            Energy -= amount;
            Changed?.Invoke();
            return true;
        }

        public void GainEnergy(int amount)
        {
            if (amount <= 0 || !Stats.IsAlive) return;
            Energy += amount;
            Changed?.Invoke();
        }

        public void ResetEnergy()
        {
            if (!Stats.IsAlive) return;
            Energy = Math.Max(0, Data.StartingEnergy);
            Changed?.Invoke();
        }
    }
}
