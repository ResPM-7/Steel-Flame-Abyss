using UnityEngine;
using UnityEngine.UI;

//플레이어 턴 시작과 종료에 필요한 전투 상태 변경
public class BattleTurnController : MonoBehaviour
{
    [SerializeField] [Min(1)] private int cardsPerTurn = 3;

    private IBattleDeckManager deckManager;
    private BattleParticipants participants;

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

    //전투 시작용 테스트 덱과 첫 손패 생성
    private void Start()
    {
        if (!ValidateReferences() || deckManager.IsReady)
            return;

        deckManager.ResetTestDeck();
    }

    //손패 교체와 플레이어 자원 갱신
    public void EndPlayerTurn()
    {
        if (!ValidateReferences() || !deckManager.IsReady || !participants.IsReady)
            return;

        int drawnCount = deckManager.DrawNewHand(cardsPerTurn);
        participants.Player.ResetEnergy();
        participants.Enemy.PlanNextAction();
        Debug.Log($"[턴] 턴 종료 / 새 손패 {drawnCount}장", this);
    }

    //턴 진행에 필요한 연결 상태 확인
    private bool ValidateReferences()
    {
        if (deckManager != null && participants != null)
            return true;

        Debug.LogError("[턴] 덱 관리자와 전투 참가자 연결을 확인해 주세요", this);
        return false;
    }
}
