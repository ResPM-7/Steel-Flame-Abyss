using System;
using System.Collections.Generic;

//후보가 남은 등급을 먼저 추첨한 뒤 해당 등급 안에서 균등 추첨
public static class RarityWeightedPicker
{
    private static readonly DataRarity[] rarities =
        { DataRarity.Starter, DataRarity.Common, DataRarity.Uncommon, DataRarity.Rare, DataRarity.Special };

    //설정 미전달은 기존 균등 추첨, 추첨 가능한 등급 부재는 -1 반환
    public static int SelectIndex<T>(IReadOnlyList<T> pool, Random random,
        Func<T, DataRarity> getRarity, RarityWeights rates)
    {
        if (pool == null || random == null || getRarity == null) throw new ArgumentNullException();
        if (pool.Count == 0) return -1;
        if (rates == null) return random.Next(pool.Count);
        double total = 0;
        foreach (var rarity in rarities)
            if (Count(pool, getRarity, rarity) > 0) total += rates.GetWeight(rarity);
        if (total <= 0) return -1;

        double roll = random.NextDouble() * total;
        DataRarity selected = default;
        foreach (var rarity in rarities)
        {
            float weight = rates.GetWeight(rarity);
            if (weight <= 0 || Count(pool, getRarity, rarity) == 0) continue;
            selected = rarity;
            roll -= weight;
            if (roll < 0) break;
        }
        int remaining = random.Next(Count(pool, getRarity, selected));
        for (int i = 0; i < pool.Count; i++)
            if (getRarity(pool[i]) == selected && remaining-- == 0) return i;
        return -1;
    }

    //추가 후보 배열 없이 등급별 남은 수 집계
    private static int Count<T>(IReadOnlyList<T> pool, Func<T, DataRarity> getRarity, DataRarity rarity)
    {
        int count = 0;
        for (int i = 0; i < pool.Count; i++)
            if (getRarity(pool[i]) == rarity) count++;
        return count;
    }
}
