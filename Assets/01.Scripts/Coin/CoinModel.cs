using System;

//보유 코인과 증감 규칙 관리
public class CoinModel
{
    public int Amount { get; private set; }
    public event Action<int> Changed;

    //음수와 최대 보유량 초과를 제외한 코인 추가
    public bool TryAdd(int amount)
    {
        if (amount <= 0 || amount > int.MaxValue - Amount)
            return false;

        Amount += amount;
        Changed?.Invoke(Amount);
        return true;
    }

    //보유량 확인 후 코인 차감
    public bool TrySpend(int amount)
    {
        if (amount <= 0 || amount > Amount)
            return false;

        Amount -= amount;
        Changed?.Invoke(Amount);
        return true;
    }

    //새 게임의 보유 코인 초기화
    public void Reset()
    {
        if (Amount == 0)
            return;

        Amount = 0;
        Changed?.Invoke(Amount);
    }
}
