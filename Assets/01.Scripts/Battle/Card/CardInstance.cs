using System;
using System.Collections.Generic;

/// <summary>
/// 카드 원본 데이터와 전투 중 변경되는 상태를 분리한 런타임 카드입니다.
/// </summary>
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

    /// <summary>
    /// 전투 중 비용을 증감합니다. 예: -1은 비용 1 감소, +1은 비용 1 증가입니다.
    /// </summary>
    public void ModifyCostForBattle(int amount)
    {
        temporaryCostModifier += amount;
    }

    public void ResetCost()
    {
        temporaryCostModifier = 0;
    }
}
