using UnityEngine;

//플레이어 턴 시작과 종료에 필요한 전투 상태 변경
public class BattleTurnController : MonoBehaviour
{
    [SerializeField] [Min(1)] private int cardsPerTurn = 3;

    private IBattleDeckManager deckManager;
    private BattleParticipants participants;
    private bool isResolvingTurn;
    private bool initializeOnStart = true;

    //맵 선택 전 자동 덱 시작 해제
    public void UseMapFlow()
    {
        initializeOnStart = false;
    }

    //외부에서 전달한 덱 관리자와 전투 참가자 저장
    public void Inject(IBattleDeckManager manager, BattleParticipants battleParticipants)
    {
        if (manager == null)
            throw new System.ArgumentNullException(nameof(manager));
        if (battleParticipants == null)
            throw new System.ArgumentNullException(nameof(battleParticipants));

        deckManager = manager;
        participants = battleParticipants;
    }

    //보유 덱 기반 전투 덱과 첫 손패 생성
    private void Start()
    {
        if (!initializeOnStart || !ValidateReferences() || deckManager.IsReady)
            return;

        deckManager.BeginBattle();
    }

    //손패 정리와 적 행동 실행 후 다음 플레이어 턴 시작
    public void EndPlayerTurn()
    {
        if (isResolvingTurn || !ValidateReferences() || !deckManager.IsReady || !participants.IsReady)
            return;
        if (!participants.IsBattleActive)
            return;

        isResolvingTurn = true;
        try
        {
            deckManager.DiscardAll();
            participants.Enemy.Stats.ClearBlock();
            participants.Enemy.ExecutePlannedAction(participants.Player.Stats);

            if (!participants.IsBattleActive)
            {
                GameLogger.Log("[턴] 전투 종료 / 다음 턴 진행 중단", this);
                return;
            }

            StartPlayerTurn();
        }
        finally
        {
            isResolvingTurn = false;
        }
    }

    //플레이어 방어도와 에너지 초기화 및 새 손패와 적 행동 준비
    private void StartPlayerTurn()
    {
        participants.Player.Stats.ClearBlock();
        participants.Player.ResetEnergy();
        participants.Enemy.PlanNextAction();
        int drawnCount = deckManager.DrawCards(cardsPerTurn);
        GameLogger.Log($"[턴] 적 행동 완료 / 내 턴 시작 / 새 손패 {drawnCount}장", this);
    }

    //턴 진행에 필요한 연결 상태 확인
    private bool ValidateReferences()
    {
        if (deckManager != null && participants != null)
            return true;

        GameLogger.LogError("[턴] 덱 관리자와 전투 참가자 연결을 확인해 주세요", this);
        return false;
    }
}
