using UnityEngine;

//카드 대상과 에너지를 검사하고 카드 효과 실행
public class CardPlayController
{
    private readonly IBattleDeckManager deckManager;
    private readonly BattleParticipants participants;
    private readonly CardEffectResolver effectResolver;

    //카드 사용에 필요한 덱과 전투 참가자 저장
    public CardPlayController(IBattleDeckManager manager, BattleParticipants battleParticipants)
    {
        if (manager == null)
            throw new System.ArgumentNullException(nameof(manager));
        if (battleParticipants == null)
            throw new System.ArgumentNullException(nameof(battleParticipants));

        deckManager = manager;
        participants = battleParticipants;
        effectResolver = new CardEffectResolver(manager, battleParticipants);
    }

    //카드 대상과 위치 및 에너지를 검사한 뒤 카드 사용
    public bool TryPlayCard(CardInstance card, BattleCombatantView targetView, bool raisedEnough)
    {
        if (!participants.IsReady || !participants.Player.Stats.IsAlive ||
            !deckManager.ContainsInHand(card))
            return false;

        bool requiresEnemyTarget = RequiresEnemyTarget(card);
        if (requiresEnemyTarget &&
            (targetView != participants.EnemyView || !participants.Enemy.Stats.IsAlive))
            return false;
        if (!requiresEnemyTarget && !raisedEnough)
            return false;
        if (!effectResolver.CanResolve(card))
        {
            Debug.LogWarning($"[카드 사용] {card.Data.DisplayName} 효과는 아직 지원하지 않습니다");
            return false;
        }
        if (participants.Player.Energy < card.Cost)
        {
            Debug.LogWarning($"[카드 사용] {card.Data.DisplayName} 카드의 에너지가 부족합니다");
            return false;
        }
        if (!participants.Player.TrySpendEnergy(card.Cost))
            return false;

        effectResolver.Resolve(card);
        bool exhaust = HasExhaustEffect(card);
        if (!deckManager.FinishPlayingCard(card, exhaust))
            return false;

        Debug.Log($"[카드 사용] {card.Data.DisplayName} 사용");
        return true;
    }

    //단일 적 선택 효과 포함 여부 확인
    private static bool RequiresEnemyTarget(CardInstance card)
    {
        for (int index = 0; index < card.Effects.Count; index++)
        {
            if (card.Effects[index].Target == TargetType.SingleEnemy)
                return true;
        }

        return false;
    }

    //사용 후 소멸 효과 포함 여부 확인
    private static bool HasExhaustEffect(CardInstance card)
    {
        for (int index = 0; index < card.Effects.Count; index++)
        {
            if (card.Effects[index].Type == EffectType.Exhaust)
                return true;
        }

        return false;
    }
}
