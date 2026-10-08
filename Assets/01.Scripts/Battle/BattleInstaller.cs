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

    //전투 초기화 전에 데이터 조회 기능 주입
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
        handController.Inject(deckManager);
        participants.Inject(dataProvider, deckManager);
        turnController.Inject(deckManager, participants);
    }
}
