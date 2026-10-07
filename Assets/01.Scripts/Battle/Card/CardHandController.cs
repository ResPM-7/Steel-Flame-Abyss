using System.Collections.Generic;
using SteelFlameAbyss.Battle;
using SteelFlameAbyss.Data;
using UnityEngine;

//카드 덱의 드로우와 버리기 결과를 미리 배치한 손패 슬롯에 표시합니다.
public class CardHandController : MonoBehaviour
{
    [SerializeField] private GameDataProvider dataProvider;
    [SerializeField] private CardHandLayout handLayout;
    [SerializeField] private RectTransform hoverLayer;
    [SerializeField] private CardView[] slots;
    [Header("임시 테스트 덱")]
    [SerializeField] private int[] testDeckIds =
    {
        10001, 10001, 10001, 10001, 10001,
        10002, 10002, 10002, 10002, 10002, 10003, 10003
    };
    [Min(0)] [SerializeField] private int initialHandCount = 3;

    private DeckState deck;
#if UNITY_EDITOR
    [Header("에디터 테스트 패널")]
    [SerializeField] private RectTransform testPanelTarget;
    private readonly Vector3[] targetCorners = new Vector3[4];
    private Canvas testCanvas;
    private int discardNumber = 1;
#endif
    public bool IsReady => deck != null;
    public int HandCount => deck?.Hand.Count ?? 0;
    public int DrawCount => deck?.DrawPile.Count ?? 0;
    public int DiscardCount => deck?.DiscardPile.Count ?? 0;

    //재생을 시작하면 테스트 덱을 만들고 초기 손패를 표시합니다.
    private void Start()
    {
#if UNITY_EDITOR
        if (testPanelTarget != null)
            testCanvas = testPanelTarget.GetComponentInParent<Canvas>();
#endif
        if (!IsReady)
            ResetTestDeck();
    }

#if UNITY_EDITOR
    //에디터 재생 중 적의 위쪽에 카드 테스트 버튼을 표시합니다.
    private void OnGUI()
    {
        if (!Application.isPlaying || testPanelTarget == null ||
            !testPanelTarget.gameObject.activeInHierarchy || testCanvas == null)
            return;

        testPanelTarget.GetWorldCorners(targetCorners);
        var camera = testCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : testCanvas.worldCamera;
        var top = RectTransformUtility.WorldToScreenPoint(camera, (targetCorners[1] + targetCorners[2]) * 0.5f);
        float width = Mathf.Min(260f, Screen.width - 8f);
        float x = Mathf.Clamp(top.x - width * 0.5f, 4f, Screen.width - width - 4f);
        float y = Mathf.Max(4f, Screen.height - top.y - 100f);

        GUILayout.BeginArea(new Rect(x, y, width, 94f), GUI.skin.box);
        GUILayout.Label($"카드 테스트  손패 {HandCount} / 뽑기 {DrawCount} / 버림 {DiscardCount}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("초기화", GUILayout.Height(24f))) ResetTestDeck();
        bool previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && IsReady;
        if (GUILayout.Button("1장 드로우", GUILayout.Height(24f))) DrawCards(1);
        if (GUILayout.Button("3장 드로우", GUILayout.Height(24f))) DrawCards(3);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        discardNumber = Mathf.Clamp(discardNumber, 1, Mathf.Max(1, HandCount));
        if (GUILayout.Button("◀", GUILayout.Width(24f), GUILayout.Height(24f)))
            discardNumber = Mathf.Max(1, discardNumber - 1);
        GUILayout.Label($"{discardNumber}번", GUILayout.Width(32f));
        if (GUILayout.Button("▶", GUILayout.Width(24f), GUILayout.Height(24f)))
            discardNumber = Mathf.Min(Mathf.Max(1, HandCount), discardNumber + 1);
        GUI.enabled = previousEnabled && IsReady && HandCount > 0;
        if (GUILayout.Button("버리기", GUILayout.Height(24f))) DiscardAt(discardNumber - 1);
        if (GUILayout.Button("전체 버리기", GUILayout.Height(24f))) DiscardAll();
        GUI.enabled = previousEnabled;
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
#endif

    //SO에서 테스트 카드 ID를 조회해 새 덱을 만들고 초기 손패를 뽑습니다.
    public void ResetTestDeck()
    {
        if (!Application.isPlaying || !ValidateReferences())
            return;

        var cards = new List<CardInstance>();
        foreach (var id in testDeckIds)
        {
            if (!dataProvider.TryGetCard(id, out var data))
            {
                Debug.LogWarning($"[카드 테스트] 카드 ID {id}가 없어 초기화를 취소했습니다.", this);
                return;
            }
            cards.Add(new CardInstance(data));
        }

        deck = new DeckState(cards);
        DrawCards(initialHandCount);
    }

    //손패 빈자리만큼 카드를 뽑고 표시를 갱신합니다.
    public void DrawCards(int count)
    {
        if (!Application.isPlaying || !IsReady)
            return;

        int requested = Mathf.Clamp(count, 0, slots.Length - HandCount);
        int drawn = deck.Draw(requested).Count;
        RefreshHand();
        LogState($"드로우 {drawn}장");
    }

    //지정한 손패 인덱스의 카드를 버린 더미로 옮깁니다.
    public void DiscardAt(int index)
    {
        if (!Application.isPlaying || !IsReady)
            return;
        if (index < 0 || index >= HandCount)
        {
            Debug.LogWarning("[카드 테스트] 버릴 카드 순번을 확인해 주세요.", this);
            return;
        }

        var card = deck.Hand[index];
        deck.Discard(card);
        RefreshHand();
        LogState($"{card.Data.DisplayName} 버리기");
    }

    //현재 손패 전체를 버린 더미로 옮깁니다.
    public void DiscardAll()
    {
        if (!Application.isPlaying || !IsReady)
            return;

        deck.DiscardHand();
        RefreshHand();
        LogState("전체 버리기");
    }

    //기존 카드를 슬롯으로 복구하고 손패 순서대로 데이터를 연결합니다.
    private void RefreshHand()
    {
        foreach (var slot in slots)
            slot.Hide();

        for (int index = 0; index < HandCount; index++)
            slots[index].Bind(deck.Hand[index], hoverLayer);

        handLayout.RefreshSpacing();
    }

    //필수 참조와 슬롯 연결을 검사해 잘못된 상태에서 덱 생성을 막습니다.
    private bool ValidateReferences()
    {
        if (dataProvider == null || !dataProvider.IsReady || handLayout == null ||
            hoverLayer == null || slots == null || slots.Length == 0 ||
            testDeckIds == null || testDeckIds.Length == 0)
        {
            Debug.LogError("[카드 테스트] 데이터, 슬롯, 호버 레이어, 테스트 덱 연결을 확인해 주세요.", this);
            return false;
        }

        var uniqueSlots = new HashSet<CardView>();
        foreach (var slot in slots)
        {
            if (slot == null || !slot.IsConfigured || !uniqueSlots.Add(slot) ||
                slot.transform.parent != handLayout.transform)
            {
                Debug.LogError("[카드 테스트] 슬롯의 텍스트 연결, 중복 또는 부모를 확인해 주세요.", this);
                return false;
            }
        }
        return true;
    }

    //드로우와 버리기 결과 및 카드 더미 개수를 Console에 표시합니다.
    private void LogState(string action)
    {
        Debug.Log($"[카드 테스트] {action} / 손패 {HandCount}, 뽑기 {DrawCount}, 버림 {DiscardCount}", this);
    }
}
