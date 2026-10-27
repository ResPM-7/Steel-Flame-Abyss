using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

public enum GameFlowState { Map, Battle, Reward, Event, Defeat, Complete, Camp, Shop }

//맵 씬의 지점 진행과 캠프 및 상점 화면 관리
[DefaultExecutionOrder(100)]
public class GameFlowController : MonoBehaviour, IScenePreparation
{
    //맵 초기화와 목록 카드 풀 준비 완료 대기
    public async UniTask PrepareSceneAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await initialization.AttachExternalCancellation(cancellationToken);
        if (!initialized) throw new System.InvalidOperationException("맵 초기화 실패");
        if (!await objectPoolManager.EnsurePoolReadyAsync("ViewCardList")
            .AsUniTask().AttachExternalCancellation(cancellationToken))
            throw new System.InvalidOperationException("목록 카드 풀 준비 실패");
    }
    [SerializeField] private GameDataProvider dataProvider;
    [SerializeField] private ObjectPoolManager objectPoolManager;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private MapPopupManager popups;
    [SerializeField] private PlayerHealthView commonHealthView;
    [SerializeField] private UnityEngine.UI.ScrollRect mapScroll;
    [SerializeField] private TMP_Text mapTitle;
    [SerializeField] private RandomMapView randomMap;
    //실행 중 생성된 노드만 보관
    private MapNodeView[] nodes;
    [SerializeField] private int playerId = 50001;

    private RunProgressModel run;
    private OwnedDeckModel ownedDeck;
    private IRunSceneLoader sceneLoader;
    private bool initialized;
    private UniTask initialization;
    public GameFlowState State { get; private set; } = GameFlowState.Map;
    public IGameDataProvider Data => dataProvider;
    //타이틀 선택 우선 적용과 맵 단독 실행 기본값
    private int ActivePlayerId => run != null && run.PlayerId > 0 ? run.PlayerId : playerId;

    //세션의 진행과 코인 및 보유 덱과 씬 이동 기능 주입
    public void InjectRun(RunProgressModel model, CoinModel coins, OwnedDeckModel cards, IRunSceneLoader loader, OwnedRelicModel relics)
    {
        run = model;
        ownedDeck = cards;
        sceneLoader = loader;
        if (popups != null) popups.Inject(model, coins, cards, dataProvider, objectPoolManager, relics);
    }

    //보유 덱과 체력 준비 및 기존 맵 진행 복원
    private void Start()
    {
        //Start와 로딩 화면에서 같은 초기화 결과 공유
        initialization = InitializeAsync(destroyCancellationToken).Preserve();
        ObserveInitializationAsync().Forget();
    }

    //맵 단독 실행에서도 초기화 실패 기록
    private async UniTask ObserveInitializationAsync()
    {
        try { await initialization; }
        catch (System.OperationCanceledException) { }
        catch (System.Exception error) { GameLogger.LogException(error, this); }
    }

    //노드 풀 로드와 배치 완료 이후 맵 입력 허용
    private async UniTask InitializeAsync(CancellationToken cancellationToken)
    {
        if (!ValidateReferences())
        {
            throw new System.InvalidOperationException("[맵] 데이터 공급자·풀·패널·Random Map View 연결과 콘텐츠 ID 확인 필요");
        }
        CharacterData character = dataProvider.GetCharacter(ActivePlayerId);
        if (!ownedDeck.IsInitialized)
        {
            //선택한 캐릭터 SO의 시작 덱 복사
            var validated = new List<int>(character.StartingDeckIds);
            if (validated.Count == 0 || validated.Exists(id => !dataProvider.TryGetCard(id, out _)))
            {
                throw new System.InvalidOperationException("[맵] 시작 덱 카드 ID 확인 필요");
            }
            ownedDeck.InitializeOnce(validated);
        }
        run.PreparePlayer(character);
        run.PrepareMap(randomMap.Generate);
        nodes = await randomMap.BuildAsync(run.Map, mapScroll, objectPoolManager, cancellationToken);
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
            sceneLoader == null || objectPoolManager == null || mapPanel == null || popups == null || !popups.IsConfigured ||
            mapScroll == null || mapScroll.content == null || mapScroll.viewport == null || mapTitle == null || randomMap == null || !randomMap.IsConfigured ||
            !dataProvider.TryGetCharacter(ActivePlayerId, out _))
            return false;
        return randomMap.ValidateContent(dataProvider, popups.HasEvent);
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
            SetPanels(false);
            popups.OpenShop(index, CompleteShop);
        }
        else if (node.NodeType == MapNodeType.Camp)
        {
            State = GameFlowState.Camp;
            SetPanels(false);
            popups.OpenCamp(CompleteCamp);
        }
        else if (node.NodeType == MapNodeType.Event)
        {
            State = GameFlowState.Event;
            SetPanels(false);
            popups.OpenEvent(index, CompleteEvent, node.Value);
        }
        else if (sceneLoader.EnterBattle(node.Value, node.NodeType == MapNodeType.Boss))
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
        SetPanels(true);
        mapTitle.text = run.Map.IsFailed ? "진행 종료" : run.Map.IsComplete ? "지도 완료" : "지도";
        for (int i = 0; i < nodes.Length; i++)
            nodes[i].Refresh(run.Map.CanEnter(i), run.Map.IsCompleted(i));
        randomMap.RefreshRoutes();
    }

    //맵 표시 전환과 생성된 팝업 닫기
    private void SetPanels(bool map)
    {
        mapPanel.SetActive(map);
        if (commonHealthView != null) commonHealthView.gameObject.SetActive(true);
        popups.CloseAll();
    }

    //화면 전환 시 카드 목록 닫기
    private void CloseCardList()
    {
        if (popups != null) popups.CloseDeck();
    }

    //상점과 맵 지점 입력 구독 해제
    private void OnDestroy()
    {
        if (nodes == null) return;
        foreach (MapNodeView node in nodes)
            if (node != null) node.Unbind();
    }
}
