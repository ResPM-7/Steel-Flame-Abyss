using UnityEngine;
using UnityEngine.SceneManagement;

//게임 실행 동안 데이터 유지와 씬 의존성 및 비동기 이동 관리
[AddComponentMenu("")]
public class GameSession : MonoBehaviour, IRunSceneLoader
{
    private readonly CoinModel coins = new CoinModel();
    private readonly OwnedDeckModel ownedDeck = new OwnedDeckModel();
    private readonly RunProgressModel run = new RunProgressModel();
    private readonly OwnedRelicModel ownedRelics = new OwnedRelicModel();
    public OwnedRelicModel OwnedRelics => ownedRelics;
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
            foreach (TitleCharacterSelectView selection in root.GetComponentsInChildren<TitleCharacterSelectView>(true))
                selection.Inject(this);
            foreach (CoinInstaller installer in root.GetComponentsInChildren<CoinInstaller>(true))
                installer.Inject(coins);
            foreach (BattleInstaller installer in root.GetComponentsInChildren<BattleInstaller>(true))
            {
                installer.InjectOwnedDeck(ownedDeck);
                installer.InjectOwnedRelics(ownedRelics);
            }
            foreach (GameFlowController flow in root.GetComponentsInChildren<GameFlowController>(true))
                flow.InjectRun(run, coins, ownedDeck, this, ownedRelics);
            foreach (BattleSceneController battle in root.GetComponentsInChildren<BattleSceneController>(true))
                battle.Inject(run, this);
            foreach (RelicPanelView relicPanel in root.GetComponentsInChildren<RelicPanelView>(true))
                relicPanel.Inject(ownedRelics);
        }
#if UNITY_EDITOR
        InjectEditorCheats(scene);
#endif
    }

    //캐릭터와 시작 덱 검증 후 새 런 준비 및 맵 이동
    public bool TryStartNewGame(CharacterData character, GameDatabase database)
    {
        if (IsLoading || sceneLoader == null || character == null || database == null ||
            !database.TryGetCharacter(character.Id, out var registered) || registered != character ||
            character.MaxHealth <= 0 || character.StartingDeckIds.Count == 0) return false;
        var deck = new System.Collections.Generic.List<int>(character.StartingDeckIds);
        foreach (int id in deck)
        {
            if (database.TryGetCard(id, out _)) continue;
            GameLogger.LogError($"[캐릭터 선택] {character.DisplayName} 시작 덱의 카드 {id} 확인", this);
            return false;
        }
        return sceneLoader.TryLoadScene(sceneLoader.MapScenePath, () =>
        {
            ResetRun();
            run.PreparePlayer(character);
            ownedDeck.InitializeOnce(deck);
        });
    }

    //전투 대상과 보스 여부 저장 및 비동기 씬 이동
    public bool EnterBattle(int enemyId, bool isFinalBoss = false)
    {
        if (IsLoading || run.IsCleared || run.Health == null || !run.Health.IsAlive || run.HasPendingBattle ||
            run.Map == null || !run.Map.IsVisiting || !sceneLoader.CanLoad(sceneLoader.BattleScenePath) ||
            !sceneLoader.CanLoad(sceneLoader.MapScenePath))
            return false;
        run.SetBattle(enemyId, isFinalBoss);
        if (sceneLoader.TryLoadScene(sceneLoader.BattleScenePath)) return true;
        run.ClearBattle();
        return false;
    }

    //승리 후 다음 지점 개방과 맵 씬 이동
    public bool ReturnToMap()
    {
        if (IsLoading || run.IsCleared || run.IsFinalBossBattle || !run.HasPendingBattle || run.Health == null || !run.Health.IsAlive ||
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
        GameDataProvider relicData = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (relicData == null) relicData = root.GetComponentInChildren<GameDataProvider>(true);
        panel.InjectRelicGrant(id => ownedRelics.TryAdd(id, relicData));
    }
#endif

    //새 게임의 코인과 덱 및 체력과 맵 초기화
    private void ResetRun()
    {
        coins.Reset();
        ownedDeck.Reset();
        ownedRelics.Reset();
        run.Reset();
    }

    //게임 세션 제거 시 씬 로드 이벤트 연결 해제
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= InjectScene;
    }
}
