using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum GameFlowState { Map, Battle, Reward, Event, Defeat, Complete, Camp, Shop }

//맵 씬의 지점 진행과 캠프 및 상점 화면 관리
[DefaultExecutionOrder(100)]
public class GameFlowController : MonoBehaviour
{
    [SerializeField] private GameDataProvider dataProvider;
    [SerializeField] private ObjectPoolManager objectPoolManager;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject eventPanel;
    [SerializeField] private CampPanelView campPanel;
    [SerializeField] private ShopPanelView shopPanel;
    [SerializeField] private PlayerHealthView commonHealthView;
    [SerializeField] private UnityEngine.UI.ScrollRect mapScroll;
    [SerializeField] private TMP_Text mapTitle;
    [Tooltip("진행할 지점 순서로 연결")]
    [SerializeField] private MapNodeView[] nodes;
    [SerializeField] private int playerId = 50001;
    [SerializeField] private CardListPanelView cardListPanel;
    [Tooltip("비어 있으면 캐릭터 SO의 시작 덱 사용")]
    [SerializeField] private int[] startingDeckIds;

    private RunProgressModel run;
    private OwnedDeckModel ownedDeck;
    private IRunSceneLoader sceneLoader;
    private ShopPresenter shopPresenter;
    private bool initialized;
    public GameFlowState State { get; private set; } = GameFlowState.Map;
    public IGameDataProvider Data => dataProvider;

    //세션의 진행과 코인 및 보유 덱과 씬 이동 기능 주입
    public void InjectRun(RunProgressModel model, CoinModel coins, OwnedDeckModel cards, IRunSceneLoader loader)
    {
        run = model;
        ownedDeck = cards;
        sceneLoader = loader;
        shopPresenter?.Dispose();
        shopPresenter = shopPanel != null && shopPanel.IsConfigured
            ? new ShopPresenter(coins, cards, shopPanel) : null;
        if (cardListPanel != null)
            cardListPanel.Inject(cards, dataProvider, null, objectPoolManager);
    }

    //보유 덱과 체력 준비 및 기존 맵 진행 복원
    private void Start()
    {
        if (!ValidateReferences())
        {
            Debug.LogError("[맵] 데이터 공급자와 풀 및 패널과 지점 목록을 연결해 주세요", this);
            return;
        }
        CharacterData character = dataProvider.GetCharacter(playerId);
        if (!ownedDeck.IsInitialized)
        {
            IEnumerable<int> ids = startingDeckIds != null && startingDeckIds.Length > 0
                ? startingDeckIds : character.StartingDeckIds;
            var validated = new List<int>(ids);
            if (validated.Count == 0 || validated.Exists(id => !dataProvider.TryGetCard(id, out _)))
            {
                Debug.LogError("[맵] 시작 덱 카드 ID를 확인해 주세요", this);
                return;
            }
            ownedDeck.InitializeOnce(validated);
        }
        run.PreparePlayer(character);
        run.PrepareMap(nodes.Length);
        if (commonHealthView != null) commonHealthView.InjectHealth(run.Health);
        for (int i = 0; i < nodes.Length; i++) nodes[i].Bind(i, SelectNode);
        initialized = true;
        ShowMap();
        Canvas.ForceUpdateCanvases();
        mapScroll.StopMovement();
        mapScroll.horizontalNormalizedPosition = run.MapScrollX;
        mapScroll.verticalNormalizedPosition = run.MapScrollY;
    }

    //맵 화면과 지점별 데이터 및 세션 연결 확인
    private bool ValidateReferences()
    {
        if (dataProvider == null || !dataProvider.IsReady || run == null || ownedDeck == null ||
            sceneLoader == null || objectPoolManager == null || mapPanel == null ||
            mapScroll == null || mapScroll.content == null || mapTitle == null ||
            nodes == null || nodes.Length == 0 || !dataProvider.TryGetCharacter(playerId, out _))
            return false;
        var unique = new HashSet<MapNodeView>();
        foreach (MapNodeView node in nodes)
        {
            if (node == null || !node.IsConfigured || !unique.Add(node)) return false;
            if (node.NodeType == MapNodeType.Event)
            {
                if (eventPanel == null) return false;
            }
            else if (node.NodeType == MapNodeType.Camp)
            {
                if (campPanel == null || !campPanel.IsConfigured) return false;
            }
            else if (node.NodeType == MapNodeType.Shop)
            {
                if (shopPresenter == null) return false;
            }
            else if (!dataProvider.TryGetEnemy(node.EnemyId, out _)) return false;
        }
        return true;
    }

    //열린 지점 선택과 전투 씬 또는 비전투 패널 진입
    private void SelectNode(int index)
    {
        if (!initialized || State != GameFlowState.Map || sceneLoader.IsLoading || !run.Map.TryEnter(index))
            return;
        CloseCardList();
        run.SaveMapScroll(mapScroll.horizontalNormalizedPosition, mapScroll.verticalNormalizedPosition);
        MapNodeView node = nodes[index];
        if (node.NodeType == MapNodeType.Shop)
        {
            State = GameFlowState.Shop;
            SetPanels(false, false);
            shopPresenter.Open(index, dataProvider.Database.Cards,
                dataProvider.GetCharacter(run.PlayerId).Class, CompleteShop);
        }
        else if (node.NodeType == MapNodeType.Camp)
        {
            State = GameFlowState.Camp;
            SetPanels(false, false);
            campPanel.Open(run.Health, CompleteCamp);
        }
        else if (node.NodeType == MapNodeType.Event)
        {
            State = GameFlowState.Event;
            SetPanels(false, true);
        }
        else if (sceneLoader.EnterBattle(node.EnemyId))
        {
            State = GameFlowState.Battle;
        }
        else
        {
            run.Map.CancelEntry();
            ShowMap();
        }
    }

    //임시 이벤트 완료와 다음 지점 개방
    public void CompleteEvent()
    {
        CompleteVisit(GameFlowState.Event);
    }

    //캠프 완료와 다음 지점 개방
    private void CompleteCamp()
    {
        CompleteVisit(GameFlowState.Camp);
    }

    //상점 완료와 다음 지점 개방
    private void CompleteShop()
    {
        CompleteVisit(GameFlowState.Shop);
    }

    //현재 방문 상태 확인과 맵 복귀
    private void CompleteVisit(GameFlowState expected)
    {
        if (initialized && State == expected && run.Map.CompleteCurrent()) ShowMap();
    }

    //맵 복귀와 누적 진행 상태 표시
    private void ShowMap()
    {
        CloseCardList();
        State = run.Map.IsFailed ? GameFlowState.Defeat :
            run.Map.IsComplete ? GameFlowState.Complete : GameFlowState.Map;
        SetPanels(true, false);
        mapTitle.text = run.Map.IsFailed ? "진행 종료" : run.Map.IsComplete ? "지도 완료" : "지도";
        for (int i = 0; i < nodes.Length; i++)
            nodes[i].Refresh(run.Map.CanEnter(i), i < run.Map.NextIndex);
    }

    //맵과 이벤트 및 비전투 패널 표시 전환
    private void SetPanels(bool map, bool eventVisible)
    {
        mapPanel.SetActive(map);
        if (commonHealthView != null) commonHealthView.gameObject.SetActive(true);
        if (eventPanel != null) eventPanel.SetActive(eventVisible);
        if (campPanel != null) campPanel.gameObject.SetActive(false);
        if (shopPanel != null) shopPanel.Close();
    }

    //화면 전환 시 카드 목록 닫기
    private void CloseCardList()
    {
        if (cardListPanel != null) cardListPanel.Close();
    }

    //상점과 맵 지점 입력 구독 해제
    private void OnDestroy()
    {
        shopPresenter?.Dispose();
        if (nodes == null) return;
        foreach (MapNodeView node in nodes)
            if (node != null) node.Unbind();
    }
}
