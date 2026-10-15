using UnityEngine;
using UnityEngine.SceneManagement;

//게임 실행 동안 데이터 유지와 씬 의존성 및 비동기 이동 관리
[AddComponentMenu("")]
public class GameSession : MonoBehaviour, IRunSceneLoader
{
    private readonly CoinModel coins = new CoinModel();
    private readonly OwnedDeckModel ownedDeck = new OwnedDeckModel();
    private readonly RunProgressModel run = new RunProgressModel();
    private SceneMenuActions sceneLoader;
    public bool IsLoading => sceneLoader != null && sceneLoader.IsLoading;

    //첫 씬 로드 전 중복 없는 게임 세션 생성
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateSession()
    {
        if (FindFirstObjectByType<GameSession>() != null) return;
        new GameObject("GameSession").AddComponent<GameSession>();
    }

    //게임 세션 유지와 공통 씬 로더 및 로드 이벤트 연결
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        sceneLoader = gameObject.AddComponent<SceneMenuActions>();
        sceneLoader.InjectNewGameAction(ResetRun);
        SceneManager.sceneLoaded += InjectScene;
    }

    //로드된 씬의 세션 데이터와 전투 및 맵과 메뉴 연결
    private void InjectScene(Scene scene, LoadSceneMode mode)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (SceneMenuActions menu in root.GetComponentsInChildren<SceneMenuActions>(true))
            {
                menu.InjectNewGameAction(ResetRun);
                menu.InjectSceneLoader(sceneLoader);
            }
        }
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (CoinInstaller installer in root.GetComponentsInChildren<CoinInstaller>(true))
                installer.Inject(coins);
            foreach (BattleInstaller installer in root.GetComponentsInChildren<BattleInstaller>(true))
                installer.InjectOwnedDeck(ownedDeck);
            foreach (GameFlowController flow in root.GetComponentsInChildren<GameFlowController>(true))
                flow.InjectRun(run, coins, ownedDeck, this);
            foreach (BattleSceneController battle in root.GetComponentsInChildren<BattleSceneController>(true))
                battle.Inject(run, this);
        }
#if UNITY_EDITOR
        InjectEditorCheats(scene);
#endif
    }

    //전투 대상 저장과 비동기 전투 씬 이동
    public bool EnterBattle(int enemyId)
    {
        if (IsLoading || run.Health == null || !run.Health.IsAlive || run.HasPendingBattle ||
            run.Map == null || !run.Map.IsVisiting || !sceneLoader.CanLoad(sceneLoader.BattleScenePath) ||
            !sceneLoader.CanLoad(sceneLoader.MapScenePath))
            return false;
        run.SetBattle(enemyId);
        if (sceneLoader.TryLoadScene(sceneLoader.BattleScenePath)) return true;
        run.ClearBattle();
        return false;
    }

    //승리 후 다음 지점 개방과 맵 씬 이동
    public bool ReturnToMap()
    {
        if (IsLoading || !run.HasPendingBattle || run.Health == null || !run.Health.IsAlive ||
            (run.Map != null && (!run.Map.IsVisiting || run.Map.IsFailed)) || !sceneLoader.CanLoad(sceneLoader.MapScenePath))
            return false;
        if (!sceneLoader.TryLoadScene(sceneLoader.MapScenePath)) return false;
        run.Map?.CompleteCurrent();
        run.ClearBattle();
        return true;
    }

#if UNITY_EDITOR
    //현재 씬 치트창에 전투 또는 비전투 세션 데이터 주입
    private void InjectEditorCheats(Scene scene)
    {
        BattleInstaller installer = null;
        GameFlowController flow = null;
        EditorCheatPanel panel = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (installer == null) installer = root.GetComponentInChildren<BattleInstaller>(true);
            if (flow == null) flow = root.GetComponentInChildren<GameFlowController>(true);
            if (panel == null) panel = root.GetComponentInChildren<EditorCheatPanel>(true);
        }
        if (panel == null) return;
        if (installer != null) installer.InjectEditorCheats(panel, coins, ownedDeck);
        else if (flow != null) panel.Inject(flow.Data, null, null, coins, ownedDeck);
        panel.InjectRunHealth(() => run.Health);
    }
#endif

    //새 게임의 코인과 덱 및 체력과 맵 초기화
    private void ResetRun()
    {
        coins.Reset();
        ownedDeck.Reset();
        run.Reset();
    }

    //게임 세션 제거 시 씬 로드 이벤트 연결 해제
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= InjectScene;
    }
}
