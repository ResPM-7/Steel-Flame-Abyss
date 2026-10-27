using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

//전투 구성 요소에 게임 데이터 공급자 연결
[DefaultExecutionOrder(-100)]
public class BattleInstaller : MonoBehaviour, IScenePreparation
{
    private bool startingRelicsApplied;

    //첫 손패·유물·유닛 연결과 목록 카드 풀 준비 확인
    public async UniTask PrepareSceneAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (participants == null || !participants.IsReady || handController == null ||
            !handController.IsDisplayReady || !startingRelicsApplied)
            throw new System.InvalidOperationException("전투 참가자 또는 첫 손패 준비 실패");
        if (objectPoolManager == null || !await objectPoolManager.EnsurePoolReadyAsync("ViewCardList")
            .AsUniTask().AttachExternalCancellation(cancellationToken))
            throw new System.InvalidOperationException("전투 목록 카드 풀 준비 실패");
    }
    [SerializeField] private GameDataProvider dataProvider;
    [SerializeField] private BattleDeckManager deckManager;
    [SerializeField] private CardHandController handController;
    [SerializeField] private BattleParticipants participants;
    [SerializeField] private BattleTurnController turnController;
    [SerializeField] private BattleResultView resultView;
    [SerializeField] private BattlePopupManager popups;
    [SerializeField] private ObjectPoolManager objectPoolManager;

    private CardPlayController cardPlayController;
    private BattleRelicEffects relicEffects;

    //세션의 보유 유물과 전투 발동 시점 연결
    public void InjectOwnedRelics(OwnedRelicModel ownedRelics)
    {
        relicEffects = new BattleRelicEffects(ownedRelics, dataProvider, participants, handController);
        deckManager.BattleStarted -= ApplyStartingRelics;
        deckManager.BattleStarted += ApplyStartingRelics;
        turnController.InjectRelics(relicEffects);
    }

    //기본 손패와 참가자 생성 후 전투 시작 및 첫 턴 유물 적용
    private void ApplyStartingRelics()
    {
        relicEffects.Apply(RelicTrigger.BattleStart);
        relicEffects.Apply(RelicTrigger.TurnStart);
        startingRelicsApplied = true;
    }

    private void OnDestroy()
    {
        if (deckManager != null) deckManager.BattleStarted -= ApplyStartingRelics;
    }

#if UNITY_EDITOR
    //통합 에디터 치트창에 전투와 세션 의존성 주입
    public void InjectEditorCheats(EditorCheatPanel view, CoinModel coins, OwnedDeckModel ownedDeck)
    {
        view.Inject(dataProvider, participants, handController, coins, ownedDeck);
    }
#endif

    //게임 세션의 보유 덱과 보상 및 목록 화면 연결
    public void InjectOwnedDeck(OwnedDeckModel ownedDeck)
    {
        if (ownedDeck == null)
            throw new System.ArgumentNullException(nameof(ownedDeck));
        if (deckManager == null || participants == null || dataProvider == null)
            return;

        deckManager.InjectOwnedDeck(ownedDeck);
        if (popups != null)
            popups.Inject(ownedDeck, dataProvider, deckManager, participants, objectPoolManager);
    }

    //전투 구성 요소에 필요한 의존성 주입
    private void Awake()
    {
        if (dataProvider == null || !dataProvider.IsReady ||
            deckManager == null || handController == null || participants == null ||
            turnController == null)
        {
            GameLogger.LogError("[전투 연결] 데이터 공급자와 전투 구성 요소를 연결해 주세요", this);
            return;
        }

        deckManager.Inject(dataProvider);
        participants.Inject(dataProvider, deckManager);
        cardPlayController = new CardPlayController(deckManager, participants);
        handController.Inject(deckManager, cardPlayController);
        turnController.Inject(deckManager, participants);
        participants.UseMapFlow();
        turnController.UseMapFlow();
        if (resultView != null)
            resultView.Inject(participants);
        else
            GameLogger.LogError("[전투 연결] 승패 패널을 표시할 Result View를 연결해 주세요", this);
    }
}
