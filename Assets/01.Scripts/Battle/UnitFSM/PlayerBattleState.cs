using System;

//캐릭터 SO와 별도로 보관하는 플레이어의 전투 상태입니다.
public class PlayerBattleState
{
    public CharacterData Data { get; }
    public CombatantState Stats { get; }
    public int Energy { get; private set; }
    public event Action Changed;

    //캐릭터 데이터로 전투용 체력과 에너지를 초기화합니다.
    public PlayerBattleState(CharacterData data)
    {
        Data = data != null ? data : throw new ArgumentNullException(nameof(data));
        Stats = new CombatantState(data.MaxHealth);
        Energy = Math.Max(0, data.StartingEnergy);
    }

    //에너지가 충분하면 소비하고 성공 여부를 반환합니다.
    public bool TrySpendEnergy(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (!Stats.IsAlive || amount > Energy) return false;
        if (amount == 0) return true;
        Energy -= amount;
        Changed?.Invoke();
        return true;
    }

    //살아 있는 플레이어의 에너지를 증가시킵니다.
    public void GainEnergy(int amount)
    {
        if (amount <= 0 || !Stats.IsAlive) return;
        Energy += amount;
        Changed?.Invoke();
    }

    //살아 있는 플레이어의 에너지를 시작 수치로 되돌립니다.
    public void ResetEnergy()
    {
        if (!Stats.IsAlive) return;
        Energy = Math.Max(0, Data.StartingEnergy);
        Changed?.Invoke();
    }
}
