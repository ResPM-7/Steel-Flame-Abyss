using UnityEngine;
using UnityEngine.UI;

//전투 결과에 따른 승리와 패배 패널 표시
public class BattleResultView : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private Button endTurnButton;

    private BattleParticipants participants;

    //전투 참가자 주입과 결과 이벤트 연결
    public void Inject(BattleParticipants battleParticipants)
    {
        if (battleParticipants == null)
            throw new System.ArgumentNullException(nameof(battleParticipants));

        if (participants != null)
            participants.ResultChanged -= RefreshResult;

        participants = battleParticipants;
        if (isActiveAndEnabled)
            participants.ResultChanged += RefreshResult;
        RefreshResult(participants.Result);
    }

    //화면 활성화 시 결과 이벤트 연결과 표시 갱신
    private void OnEnable()
    {
        if (participants == null)
            return;

        participants.ResultChanged -= RefreshResult;
        participants.ResultChanged += RefreshResult;
        RefreshResult(participants.Result);
    }

    //화면 비활성화 시 결과 이벤트 연결 해제
    private void OnDisable()
    {
        if (participants != null)
            participants.ResultChanged -= RefreshResult;
    }

    //승패 패널 표시와 턴 종료 버튼 상태 갱신
    private void RefreshResult(BattleResult result)
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(result == BattleResult.Defeat);
        if (winPanel != null)
            winPanel.SetActive(result == BattleResult.Victory);

        GameObject visiblePanel = result == BattleResult.Defeat ? gameOverPanel :
            result == BattleResult.Victory ? winPanel : null;
        if (visiblePanel != null)
            visiblePanel.transform.SetAsLastSibling();
        if (endTurnButton != null)
            endTurnButton.interactable = result == BattleResult.Ongoing;
    }
}
