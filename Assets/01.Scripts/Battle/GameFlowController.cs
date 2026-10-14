using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum GameFlowState { Map, Battle, Reward, Event, Defeat, Complete, Camp }

//고정 맵과 전투 및 보상 화면 전환 관리
[DefaultExecutionOrder(100)]
public class GameFlowController : MonoBehaviour
{
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private GameObject handRoot;
    [SerializeField] private GameObject eventPanel;
    [SerializeField] private CampPanelView campPanel;
    [SerializeField] private PlayerHealthView commonHealthView;
    [SerializeField] private UnityEngine.UI.ScrollRect mapScroll;
    [SerializeField] private TMP_Text mapTitle;
    [Tooltip("아래 지점부터 위 지점 순서로 연결")]
    [SerializeField] private MapNodeView[] nodes;
    [SerializeField] private int playerId = 50001;
    [SerializeField] private CardListPanelView cardListPanel;

    private IGameDataProvider data;
    private IBattleDeckManager deck;
    private BattleParticipants participants;
    private MapProgressModel progress;
    private bool initialized;
    public GameFlowState State { get; private set; } = GameFlowState.Map;

    //맵 진행 데이터와 전투 기능 및 공통 체력 화면 연결
    public void Inject(IGameDataProvider provider, IBattleDeckManager manager, BattleParticipants battleParticipants)
    {
        if (provider == null || manager == null || battleParticipants == null)
            throw new System.ArgumentNullException();
        if (participants != null)
            participants.ResultChanged -= OnBattleResult;
        data = provider;
        deck = manager;
        participants = battleParticipants;
        participants.ResultChanged += OnBattleResult;
        if (commonHealthView != null)
            commonHealthView.Inject(participants);
    }

    //고정 경로 준비와 시작 지점 표시
    private void Start()
    {
        if (!ValidateReferences())
        {
            Debug.LogError("[맵] 설치자와 화면 및 아래부터 정렬한 지점 목록을 연결해 주세요", this);
            return;
        }
        if (!participants.PrepareMapPlayer(playerId) || !deck.BeginBattle())
            return;
        progress = new MapProgressModel(nodes.Length);
        for (int i = 0; i < nodes.Length; i++)
            nodes[i].Bind(i, SelectNode);
        initialized = true;
        participants.LeaveBattle();
        ShowMap();
        Canvas.ForceUpdateCanvases();
        mapScroll.StopMovement();
        mapScroll.verticalNormalizedPosition = 0f;
    }

    //화면 연결과 지점 중복 및 전투 데이터 확인
    private bool ValidateReferences()
    {
        if (data == null || !data.IsReady || deck == null || participants == null ||
            mapPanel == null || battlePanel == null || handRoot == null ||
            mapScroll == null || mapScroll.content == null || mapTitle == null ||
            nodes == null || nodes.Length == 0 || !data.TryGetCharacter(playerId, out _))
            return false;
        var unique = new HashSet<MapNodeView>();
        foreach (MapNodeView node in nodes)
        {
            if (node == null || !node.IsConfigured || !unique.Add(node))
                return false;
            if (node.NodeType == MapNodeType.Event)
            {
                if (eventPanel == null)
                    return false;
            }
            else if (node.NodeType == MapNodeType.Camp)
            {
                if (campPanel == null || !campPanel.IsConfigured)
                {
                    Debug.LogError("[캠프] Camp Panel과 상태 텍스트 및 휴식과 계속 버튼을 연결해 주세요", this);
                    return false;
                }
            }
            else if (!data.TryGetEnemy(node.EnemyId, out _))
                return false;
        }
        return true;
    }

    //열린 지점 선택과 전투 및 이벤트 또는 캠프 진입
    private void SelectNode(int index)
    {
        if (!initialized || State != GameFlowState.Map || !progress.TryEnter(index))
            return;
        CloseCardList();
        MapNodeView node = nodes[index];
        if (node.NodeType == MapNodeType.Camp)
        {
            State = GameFlowState.Camp;
            SetPanels(false, false, false);
            campPanel.Open(participants.Player.Stats, CompleteCamp);
            return;
        }
        if (node.NodeType == MapNodeType.Event)
        {
            State = GameFlowState.Event;
            SetPanels(false, false, true);
            return;
        }
        if (!participants.Initialize(playerId, node.EnemyId, true) || !deck.BeginBattle())
        {
            participants.LeaveBattle();
            progress.CancelEntry();
            ShowMap();
            return;
        }
        State = GameFlowState.Battle;
        SetPanels(false, true, false);
    }

    //승리 보상 대기와 패배 진행 차단
    private void OnBattleResult(BattleResult result)
    {
        if (!initialized || State != GameFlowState.Battle)
            return;
        if (result == BattleResult.Victory)
            State = GameFlowState.Reward;
        else if (result == BattleResult.Defeat)
        {
            State = GameFlowState.Defeat;
            progress.Fail();
        }
    }

    //승리 보상 화면 종료와 다음 지점 개방
    public void ContinueAfterReward()
    {
        if (!initialized || State != GameFlowState.Reward || participants.Result != BattleResult.Victory)
            return;
        if (!progress.CompleteCurrent())
            return;
        participants.LeaveBattle();
        ShowMap();
    }

    //임시 이벤트 완료와 다음 지점 개방
    public void CompleteEvent()
    {
        if (!initialized || State != GameFlowState.Event || !progress.CompleteCurrent())
            return;
        ShowMap();
    }

    //캠프 휴식 완료와 다음 지점 개방
    private void CompleteCamp()
    {
        if (!initialized || State != GameFlowState.Camp || !progress.CompleteCurrent())
            return;
        ShowMap();
    }

    //맵 복귀와 진행 상태 표시
    private void ShowMap()
    {
        CloseCardList();
        State = progress.IsComplete ? GameFlowState.Complete : GameFlowState.Map;
        SetPanels(true, false, false);
        mapTitle.text = progress.IsComplete ? "지도 완료" : "지도";
        for (int i = 0; i < nodes.Length; i++)
            nodes[i].Refresh(progress.CanEnter(i), i < progress.NextIndex);
    }

    //현재 화면의 패널과 손패 및 전투 외 공통 체력 표시
    private void SetPanels(bool map, bool battle, bool eventVisible)
    {
        mapPanel.SetActive(map);
        battlePanel.SetActive(battle);
        handRoot.SetActive(battle);
        if (commonHealthView != null)
            commonHealthView.gameObject.SetActive(!battle);
        if (eventPanel != null)
            eventPanel.SetActive(eventVisible);
        if (campPanel != null)
            campPanel.gameObject.SetActive(false);
    }

    //화면 전환 시 열린 카드 목록 닫기
    private void CloseCardList()
    {
        if (cardListPanel != null)
            cardListPanel.Close();
    }

    //전투 결과와 지점 입력 구독 해제
    private void OnDestroy()
    {
        if (participants != null)
            participants.ResultChanged -= OnBattleResult;
        if (nodes == null)
            return;
        foreach (MapNodeView node in nodes)
            if (node != null)
                node.Unbind();
    }
}
