using System;

//전투 코인 보상 금액과 중복 수령 관리
public class CoinReward
{
    public int Amount { get; }
    public bool IsClaimed { get; private set; }

    //전투 코인 보상 금액 저장
    public CoinReward(int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));
        Amount = amount;
    }

    //코인 보상 일회 지급과 중복 요청 차단
    public bool TryClaim(CoinModel model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));
        if (IsClaimed || Amount == 0)
            return false;

        IsClaimed = true;
        if (model.TryAdd(Amount))
            return true;

        IsClaimed = false;
        return false;
    }
}
