using System.Collections.Generic;
using UnityEngine;

//카드 덱의 드로우와 버리기 결과를 미리 배치한 손패 슬롯에 표시
public class CardHandController : MonoBehaviour
{
    private IBattleDeckManager deckManager;
    private CardPlayController cardPlayController;

    [SerializeField] private CardHandLayout handLayout;
    [SerializeField] private RectTransform hoverLayer;
    [SerializeField] private CardView[] slots;
    [Header("단일 적 타겟팅")]
    [SerializeField] private RectTransform cardCenter;
    [SerializeField] private CardTargetArrowView targetArrow;

#if UNITY_EDITOR
    //손패 빈자리 확인과 치트 카드 추가
    public bool TryAddCheatCard(int cardId)
    {
        CacheSlots();
        return Application.isPlaying && IsReady && slots != null && HandCount < slots.Length &&
            deckManager.TryAddCardToHand(cardId, out _);
    }

#endif

    public bool IsReady => deckManager != null && deckManager.IsReady;
    //첫 손패 전체의 실제 표시 연결 확인
    public bool IsDisplayReady
    {
        get
        {
            if (!IsReady || slots == null || handLayout == null || hoverLayer == null || HandCount > slots.Length) return false;
            for (int i = 0; i < HandCount; i++)
                if (slots[i] == null || !slots[i].IsConfigured || slots[i].Card != deckManager.Hand[i] || !slots[i].gameObject.activeInHierarchy) return false;
            return true;
        }
    }
    public int HandCount => deckManager?.HandCount ?? 0;
    public int DrawCount => deckManager?.DrawCount ?? 0;
    public int DiscardCount => deckManager?.DiscardCount ?? 0;


    //외부에서 전달한 전투 덱과 카드 사용 기능 저장
    public void Inject(IBattleDeckManager manager, CardPlayController playController)
    {
        if (manager == null)
            throw new System.ArgumentNullException(nameof(manager));
        if (playController == null)
            throw new System.ArgumentNullException(nameof(playController));

        if (deckManager != null)
            deckManager.Changed -= RefreshHand;

        if (cardPlayController != null)
            cardPlayController.DescriptionsChanged -= RefreshDescriptions;

        CacheSlots();
        deckManager = manager;
        cardPlayController = playController;
        deckManager.Changed += RefreshHand;
        cardPlayController.DescriptionsChanged += RefreshDescriptions;
    }

    //컴포넌트 제거 시 덱과 설명 변경 이벤트 연결 해제
    private void OnDestroy()
    {
        if (deckManager != null)
            deckManager.Changed -= RefreshHand;
        if (cardPlayController != null)
            cardPlayController.DescriptionsChanged -= RefreshDescriptions;
    }

    //손패 빈자리만큼 카드를 뽑고 표시를 갱신
    public void DrawCards(int count)
    {
        if (!Application.isPlaying || !IsReady)
            return;

        int requested = Mathf.Clamp(count, 0, slots.Length - HandCount);
        int drawn = deckManager.DrawCards(requested);
        LogState($"드로우 {drawn}장");
    }

    //지정한 손패 인덱스의 카드를 버린 더미로 이동
    public void DiscardAt(int index)
    {
        if (!Application.isPlaying || !IsReady)
            return;
        if (index < 0 || index >= HandCount)
        {
            GameLogger.LogWarning("[카드 테스트] 버릴 카드 순번을 확인해 주세요.", this);
            return;
        }

        var card = deckManager.Hand[index];
        if (deckManager.DiscardAt(index))
            LogState($"{card.Data.DisplayName} 버리기");
    }

    //현재 손패 전체를 버린 더미로 이동
    public void DiscardAll()
    {
        if (!Application.isPlaying || !IsReady)
            return;

        deckManager.DiscardAll();
        LogState("전체 버리기");
    }

    //기존 카드를 슬롯으로 복구하고 손패 순서대로 데이터를 연결
    private void RefreshHand()
    {
        CacheSlots();
        if (slots == null || slots.Length == 0 || handLayout == null || hoverLayer == null)
        {
            GameLogger.LogError("[카드 손패] 슬롯과 레이아웃 연결을 확인해 주세요", this);
            return;
        }

        for (int index = 0; index < slots.Length; index++)
        {
            if (slots[index] != null)
                slots[index].Hide();
        }

        if (IsReady)
        {
            int visibleCount = Mathf.Min(HandCount, slots.Length);
            for (int index = 0; index < visibleCount; index++)
            {
                if (slots[index] != null)
                {
                    slots[index].SetTargetingReferences(cardCenter, targetArrow);
                    slots[index].Bind(deckManager.Hand[index], hoverLayer, cardPlayController);
                }
            }
        }

        handLayout.RefreshSpacing();
    }

    //힘과 약화 및 대상 상태 변경에 따른 손패 설명 갱신
    private void RefreshDescriptions()
    {
        if (slots == null)
            return;

        foreach (var slot in slots)
        {
            if (slot != null)
                slot.RefreshDescription();
        }
    }

    //비어 있는 슬롯 배열을 자식 카드 슬롯으로 한 번 캐싱
    private void CacheSlots()
    {
        bool needsCache = slots == null || slots.Length == 0;
        if (!needsCache)
        {
            for (int index = 0; index < slots.Length; index++)
            {
                if (slots[index] == null)
                {
                    needsCache = true;
                    break;
                }
            }
        }

        if (needsCache)
            slots = GetComponentsInChildren<CardView>(true);
    }

    //필수 참조와 슬롯 연결을 검사해 잘못된 상태에서 덱 생성을 방지
    private bool ValidateReferences()
    {
        CacheSlots();
        if (deckManager == null || cardPlayController == null || handLayout == null || hoverLayer == null ||
            slots == null || slots.Length == 0)
        {
            GameLogger.LogError("[카드 테스트] 덱 관리자와 손패 UI 연결을 확인해 주세요.", this);
            return false;
        }

        var uniqueSlots = new HashSet<CardView>();
        foreach (var slot in slots)
        {
            if (slot == null || !slot.IsConfigured || !uniqueSlots.Add(slot) ||
                slot.transform.parent != handLayout.transform)
            {
                GameLogger.LogError("[카드 테스트] 슬롯의 텍스트 연결, 중복 또는 부모를 확인해 주세요.", this);
                return false;
            }
        }
        return true;
    }

    //드로우와 버리기 결과 및 카드 더미 개수를 Console에 표시
    private void LogState(string action)
    {
        GameLogger.Log($"[카드 테스트] {action} / 손패 {HandCount}, 뽑기 {DrawCount}, 버림 {DiscardCount}", this);
    }

}
