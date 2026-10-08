using System.Collections.Generic;
using UnityEngine;

//카드 덱의 드로우와 버리기 결과를 미리 배치한 손패 슬롯에 표시
public class CardHandController : MonoBehaviour
{
    private IBattleDeckManager deckManager;

    [SerializeField] private CardHandLayout handLayout;
    [SerializeField] private RectTransform hoverLayer;
    [SerializeField] private CardView[] slots;

#if UNITY_EDITOR
    [Header("에디터 테스트 패널")]
    [SerializeField] private RectTransform testPanelTarget;
    private readonly Vector3[] targetCorners = new Vector3[4];
    private Canvas testCanvas;
    private int discardNumber = 1;
    private bool isCheatPanelOpen;
    private string cardIdInput = "10001";
    private string cheatMessage = "카드 ID를 입력해 손패에 추가할 수 있습니다.";
#endif

    public bool IsReady => deckManager != null && deckManager.IsReady;
    public int HandCount => deckManager?.HandCount ?? 0;
    public int DrawCount => deckManager?.DrawCount ?? 0;
    public int DiscardCount => deckManager?.DiscardCount ?? 0;


    //외부에서 전달한 전투 덱 관리자 저장
    public void Inject(IBattleDeckManager manager)
    {
        if (manager == null)
            throw new System.ArgumentNullException(nameof(manager));

        if (deckManager != null)
            deckManager.Changed -= RefreshHand;

        deckManager = manager;
        deckManager.Changed += RefreshHand;
    }

    //재생을 시작하면 테스트 덱을 만들고 초기 손패를 표시
    private void Start()
    {
        #region 에디터 전용 치트창
#if UNITY_EDITOR
        if (testPanelTarget != null)
            testCanvas = testPanelTarget.GetComponentInParent<Canvas>();
#endif
        #endregion

        if (!IsReady)
            ResetTestDeck();
    }

    //컴포넌트 제거 시 덱 변경 이벤트 연결 해제
    private void OnDestroy()
    {
        if (deckManager != null)
            deckManager.Changed -= RefreshHand;
    }
    #region 에디터 전용 
#if UNITY_EDITOR
    //에디터 재생 중 적의 위쪽에 치트 버튼과 선택된 테스트 패널을 표시
    private void OnGUI()
    {
        if (!Application.isPlaying || testPanelTarget == null ||
            !testPanelTarget.gameObject.activeInHierarchy || testCanvas == null)
            return;

        testPanelTarget.GetWorldCorners(targetCorners);
        var camera = testCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : testCanvas.worldCamera;
        var top = RectTransformUtility.WorldToScreenPoint(camera, (targetCorners[1] + targetCorners[2]) * 0.5f);
        float buttonWidth = 70f;
        float buttonHeight = 24f;
        float buttonX = Mathf.Clamp(top.x - buttonWidth * 0.5f, 4f, Screen.width - buttonWidth - 4f);
        float buttonY = Mathf.Max(4f, Screen.height - top.y - buttonHeight - 6f);

        GUI.depth = -100;
        if (!isCheatPanelOpen)
        {
            if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "치트"))
                isCheatPanelOpen = true;
            return;
        }

        float panelWidth = Mathf.Min(260f, Screen.width - 8f);
        float panelX = Mathf.Clamp(top.x - panelWidth * 0.5f, 4f, Screen.width - panelWidth - 4f);
        float panelHeight = 156f;
        float panelY = Mathf.Max(4f, buttonY - panelHeight - 4f);
        DrawCheatPanel(new Rect(panelX, panelY, panelWidth, panelHeight));
    }

    //열린 치트 패널에 카드 테스트 상태와 실행 버튼을 표시
    private void DrawCheatPanel(Rect panelRect)
    {
        GUILayout.BeginArea(panelRect, GUI.skin.box);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"카드 치트  손패 {HandCount} / 뽑기 {DrawCount} / 버림 {DiscardCount}");
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("닫기", GUILayout.Width(44f), GUILayout.Height(20f)))
        {
            isCheatPanelOpen = false;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("초기화", GUILayout.Height(24f))) ResetTestDeck();
        bool previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && IsReady;
        if (GUILayout.Button("1장 드로우", GUILayout.Height(24f))) DrawCards(1);
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

        GUILayout.BeginHorizontal();
        GUILayout.Label("카드 ID", GUILayout.Width(48f));
        cardIdInput = GUILayout.TextField(cardIdInput, 10, GUILayout.Height(24f));
        GUI.enabled = previousEnabled && IsReady;
        if (GUILayout.Button("손패에 추가", GUILayout.Width(82f), GUILayout.Height(24f)))
            AddCardToHandFromCheatInput();
        GUI.enabled = previousEnabled;
        GUILayout.EndHorizontal();
        GUILayout.Label(cheatMessage);
        GUILayout.EndArea();
    }

    //입력한 카드 ID를 조회해 손패에 바로 추가
    private void AddCardToHandFromCheatInput()
    {
        if (!int.TryParse(cardIdInput, out int cardId))
        {
            cheatMessage = "카드 ID는 숫자로 입력해 주세요.";
            return;
        }

        if (HandCount >= slots.Length)
        {
            cheatMessage = $"손패가 {slots.Length}장으로 가득 찼습니다.";
            return;
        }

        if (!deckManager.TryAddCardToHand(cardId, out CardInstance card))
        {
            cheatMessage = $"카드 ID {cardId}를 찾을 수 없습니다.";
            return;
        }

        cheatMessage = $"{card.Data.DisplayName} 카드를 손패에 추가했습니다.";
        LogState($"{card.Data.DisplayName} 손패 추가");
    }
#endif

    //전투 덱 관리자에 테스트 덱 초기화 요청
    public void ResetTestDeck()
    {
        if (!Application.isPlaying || !ValidateReferences())
            return;

        if (deckManager.ResetTestDeck())
            LogState("테스트 덱 초기화");
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
            Debug.LogWarning("[카드 테스트] 버릴 카드 순번을 확인해 주세요.", this);
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
        foreach (var slot in slots)
            slot.Hide();

        if (IsReady)
        {
            int visibleCount = Mathf.Min(HandCount, slots.Length);
            for (int index = 0; index < visibleCount; index++)
                slots[index].Bind(deckManager.Hand[index], hoverLayer);
        }

        handLayout.RefreshSpacing();
    }

    //필수 참조와 슬롯 연결을 검사해 잘못된 상태에서 덱 생성을 방지
    private bool ValidateReferences()
    {
        if (deckManager == null || handLayout == null || hoverLayer == null ||
            slots == null || slots.Length == 0)
        {
            Debug.LogError("[카드 테스트] 덱 관리자와 손패 UI 연결을 확인해 주세요.", this);
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

    //드로우와 버리기 결과 및 카드 더미 개수를 Console에 표시
    private void LogState(string action)
    {
        Debug.Log($"[카드 테스트] {action} / 손패 {HandCount}, 뽑기 {DrawCount}, 버림 {DiscardCount}", this);
    }

    #endregion 에디터 전용 치트
}
