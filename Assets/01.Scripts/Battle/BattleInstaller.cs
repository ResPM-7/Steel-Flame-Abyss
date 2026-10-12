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
    [SerializeField] private CardRewardView cardRewardView;

    private CardPlayController cardPlayController;
    private CardRewardPresenter cardRewardPresenter;

    //게임 세션의 보유 덱과 카드 보상 화면 연결
    public void InjectOwnedDeck(OwnedDeckModel ownedDeck)
    {
        if (ownedDeck == null)
            throw new System.ArgumentNullException(nameof(ownedDeck));
        if (deckManager == null || participants == null || dataProvider == null)
            return;

        deckManager.InjectOwnedDeck(ownedDeck);
        cardRewardPresenter?.Dispose();
        cardRewardPresenter = null;
        if (cardRewardView == null || !cardRewardView.ValidateReferences())
        {
            Debug.LogError("[카드 보상] Card Reward View와 화면 연결을 확인해 주세요", this);
            return;
        }
        cardRewardPresenter = new CardRewardPresenter(dataProvider, participants, ownedDeck, cardRewardView);
    }

    //씬 종료 시 카드 보상 이벤트 구독 해제
    private void OnDestroy()
    {
        cardRewardPresenter?.Dispose();
    }

    //전투 구성 요소에 필요한 의존성 주입
    private void Awake()
    {
        if (dataProvider == null || !dataProvider.IsReady ||
            deckManager == null || handController == null || participants == null ||
            turnController == null)
        {
            Debug.LogError("[전투 연결] 데이터 공급자와 전투 구성 요소를 연결해 주세요", this);
            return;
        }

        deckManager.Inject(dataProvider);
        participants.Inject(dataProvider, deckManager);
        cardPlayController = new CardPlayController(deckManager, participants);
        handController.Inject(deckManager, cardPlayController);
        turnController.Inject(deckManager, participants);
        if (resultView != null)
            resultView.Inject(participants);
        else
            Debug.LogError("[전투 연결] 승패 패널을 표시할 Result View를 연결해 주세요", this);
    }
}
