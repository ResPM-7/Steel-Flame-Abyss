using UnityEngine;

//전투 구성 요소에 게임 데이터 공급자 연결
[DefaultExecutionOrder(-100)]
public class BattleInstaller : MonoBehaviour
{
    [SerializeField] private GameDataProvider dataProvider;
    [SerializeField] private BattleDeckManager deckManager;
    [SerializeField] private CardHandController handController;
    [SerializeField] private BattleParticipants participants;
    [SerializeField] private BattleTurnController turnController;
    [SerializeField] private BattleResultView resultView;
    [SerializeField] private BattlePopupManager popups;
    [SerializeField] private ObjectPoolManager objectPoolManager;

    private CardPlayController cardPlayController;

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
