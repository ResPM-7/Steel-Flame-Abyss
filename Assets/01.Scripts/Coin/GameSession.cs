using UnityEngine;
using UnityEngine.SceneManagement;

//게임 실행 동안 코인과 보유 덱 유지 및 씬 의존성 주입
[AddComponentMenu("")]
public class GameSession : MonoBehaviour
{
    private readonly CoinModel coins = new CoinModel();
    private readonly OwnedDeckModel ownedDeck = new OwnedDeckModel();

    //첫 씬 로드 전 게임 세션 자동 생성
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateSession()
    {
        GameObject sessionObject = new GameObject("GameSession");
        sessionObject.AddComponent<GameSession>();
    }

    //게임 세션 유지와 씬 로드 이벤트 연결
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += InjectScene;
    }

    //로드된 씬의 코인과 전투 및 상점과 메뉴 및 에디터 치트 연결
    private void InjectScene(Scene scene, LoadSceneMode mode)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (CoinInstaller installer in root.GetComponentsInChildren<CoinInstaller>(true))
                installer.Inject(coins);
            foreach (BattleInstaller installer in root.GetComponentsInChildren<BattleInstaller>(true))
                installer.InjectOwnedDeck(ownedDeck);
            foreach (GameFlowController flow in root.GetComponentsInChildren<GameFlowController>(true))
                flow.InjectShop(coins, ownedDeck);
            foreach (SceneMenuActions menu in root.GetComponentsInChildren<SceneMenuActions>(true))
                menu.InjectNewGameAction(ResetRun);
        }
#if UNITY_EDITOR
        InjectEditorCheats(scene);
#endif
    }

#if UNITY_EDITOR
    //에디터 씬의 치트 오브젝트 준비와 전투 설치자 연결
    private void InjectEditorCheats(Scene scene)
    {
        BattleInstaller installer = null;
        GameObject host = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (installer == null)
                installer = root.GetComponentInChildren<BattleInstaller>(true);
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (string.Equals(child.name, "Editorcheat", System.StringComparison.OrdinalIgnoreCase))
                    host = child.gameObject;
        }
        if (installer == null)
            return;
        if (host == null)
        {
            host = new GameObject("Editorcheat");
            SceneManager.MoveGameObjectToScene(host, scene);
        }
        EditorCheatPanel panel = host.GetComponent<EditorCheatPanel>();
        if (panel == null)
            panel = host.AddComponent<EditorCheatPanel>();
        installer.InjectEditorCheats(panel, coins, ownedDeck);
    }
#endif

    //새 게임의 코인과 보유 덱 초기화
    private void ResetRun()
    {
        coins.Reset();
        ownedDeck.Reset();
    }

    //게임 세션 제거 시 씬 로드 이벤트 연결 해제
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= InjectScene;
    }
}
