using System;
using System.Collections.Generic;

//카드 원본 데이터와 전투 중 변경되는 상태를 분리한 런타임 카드
public class CardInstance
{
    private int temporaryCostModifier;

    public CardData Data { get; }
    public bool IsUpgraded { get; private set; }
    public int Cost => Math.Max(0, Data.Cost + temporaryCostModifier);

    public string Description =>
        IsUpgraded && !string.IsNullOrWhiteSpace(Data.UpgradedDescription)
            ? Data.UpgradedDescription
            : Data.Description;

    public IReadOnlyList<EffectSpec> Effects =>
        IsUpgraded && Data.UpgradedEffects.Count > 0
            ? Data.UpgradedEffects
            : Data.Effects;

    public CardInstance(CardData data, bool isUpgraded = false)
    {
        Data = data != null ? data : throw new ArgumentNullException(nameof(data));
        IsUpgraded = isUpgraded;
    }

    public void Upgrade()
    {
        IsUpgraded = true;
    }

    //전투 중 카드 비용 증감
    public void ModifyCostForBattle(int amount)
    {
        temporaryCostModifier += amount;
    }

    public void ResetCost()
    {
        temporaryCostModifier = 0;
    }
}
