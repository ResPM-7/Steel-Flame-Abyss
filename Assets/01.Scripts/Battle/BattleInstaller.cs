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

    private CardPlayController cardPlayController;

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
    }
}
