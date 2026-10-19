using UnityEngine;

//전투 씬의 진입 데이터 적용과 보상 종료 후 맵 복귀
[DefaultExecutionOrder(100)]
public class BattleSceneController : MonoBehaviour
{
    [SerializeField] private GameDataProvider dataProvider;
    [SerializeField] private BattleParticipants participants;
    [SerializeField] private BattleDeckManager deckManager;
    [SerializeField] private UnityEngine.UI.Button nextButton;
    [Header("배틀 씬 직접 실행 테스트")]
    [SerializeField] private int testPlayerId = 50001;
    [SerializeField] private int testEnemyId = 60001;
    private RunProgressModel run;
    private IRunSceneLoader loader;

    //세션 진행 데이터와 씬 이동 기능 주입
    public void Inject(RunProgressModel model, IRunSceneLoader sceneLoader)
    {
        run = model;
        loader = sceneLoader;
    }

    //선택한 적과 저장 체력으로 전투 시작 및 다음 버튼 연결
    private void Start()
    {
        if (run == null || loader == null || dataProvider == null || participants == null ||
            deckManager == null || nextButton == null)
        {
            GameLogger.LogError("[전투 씬] 세션과 데이터 및 참가자와 덱 및 다음 버튼을 연결해 주세요", this);
            return;
        }
        if (!run.HasPendingBattle)
        {
            run.PreparePlayer(dataProvider.GetCharacter(testPlayerId));
            run.SetBattle(testEnemyId);
        }
        participants.ResultChanged += OnResult;
        nextButton.onClick.AddListener(ContinueAfterReward);
        nextButton.interactable = false;
        if (!participants.Initialize(run.PlayerId, run.PendingEnemyId, false, run.Health.CurrentHealth) ||
            !deckManager.BeginBattle())
            GameLogger.LogError("[전투 씬] 전투 초기화 실패 데이터와 시작 덱을 확인해 주세요", this);
    }

    //전투 종료 체력 저장과 패배 진행 차단
    private void OnResult(BattleResult result)
    {
        nextButton.interactable = result == BattleResult.Victory;
        if (result != BattleResult.Victory && result != BattleResult.Defeat) return;
        run.CaptureHealth(participants.Player.Stats);
        if (result == BattleResult.Defeat) run.Map?.Fail();
    }

    //승리 보상 종료 확인과 중복 맵 이동 차단
    public void ContinueAfterReward()
    {
        if (loader == null || loader.IsLoading || participants.Result != BattleResult.Victory) return;
        run.CaptureHealth(participants.Player.Stats);
        if (loader.ReturnToMap()) nextButton.interactable = false;
    }

    //전투 결과와 다음 버튼 구독 해제
    private void OnDestroy()
    {
        if (participants != null) participants.ResultChanged -= OnResult;
        if (nextButton != null) nextButton.onClick.RemoveListener(ContinueAfterReward);
    }
}
