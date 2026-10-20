using System;
using System.Collections.Generic;

//전투별 보상 후보와 카드 수령 상태 관리
public class CardRewardModel
{
    private readonly List<CardData> candidates = new List<CardData>();
    public IReadOnlyList<CardData> Candidates => candidates.AsReadOnly();
    public bool IsClaimed { get; private set; }

    //캐릭터 카드와 공용 카드에서 중복 없는 후보 추첨
    public CardRewardModel(IEnumerable<CardData> cards, CharacterClass owner, Random random)
    {
        if (cards == null || random == null)
            throw new ArgumentNullException();
        var pool = new List<CardData>();
        var ids = new HashSet<int>();
        foreach (CardData card in cards)
        {
            if (card == null || card.Id <= 0 || card.CardType == CardType.Status ||
                (card.Owner != owner && card.Owner != CharacterClass.Common) || !ids.Add(card.Id))
                continue;
            pool.Add(card);
        }

        while (pool.Count > 0 && candidates.Count < 3)
        {
            int index = random.Next(pool.Count);
            candidates.Add(pool[index]);
            pool.RemoveAt(index);
        }
    }

    //유효한 후보 한 장을 보유 덱에 일회 추가
    public bool TryClaim(int index, OwnedDeckModel deck)
    {
        if (deck == null || !deck.IsInitialized || IsClaimed || index < 0 || index >= candidates.Count)
            return false;
        IsClaimed = true;
        deck.Add(candidates[index].Id);
        return true;
    }
}
