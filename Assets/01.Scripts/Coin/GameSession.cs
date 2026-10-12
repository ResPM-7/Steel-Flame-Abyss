using UnityEngine;
using UnityEngine.SceneManagement;

//게임 실행 동안 코인 모델 유지와 씬 의존성 주입
[AddComponentMenu("")]
public class GameSession : MonoBehaviour
{
    private readonly CoinModel coins = new CoinModel();

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

    //로드된 씬의 코인 설치자와 메뉴에 의존성 전달
    private void InjectScene(Scene scene, LoadSceneMode mode)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (CoinInstaller installer in root.GetComponentsInChildren<CoinInstaller>(true))
                installer.Inject(coins);
            foreach (SceneMenuActions menu in root.GetComponentsInChildren<SceneMenuActions>(true))
                menu.InjectNewGameAction(coins.Reset);
        }
    }

    //게임 세션 제거 시 씬 로드 이벤트 연결 해제
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= InjectScene;
    }
}
