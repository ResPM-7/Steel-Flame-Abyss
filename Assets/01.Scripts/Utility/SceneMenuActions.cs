using UnityEngine;
using UnityEngine.SceneManagement;

//메뉴 버튼의 게임 시작과 타이틀 이동 및 종료 처리
public class SceneMenuActions : MonoBehaviour
{
    [SerializeField] private int titleSceneIndex = 0;
    [UnityEngine.Serialization.FormerlySerializedAs("gameSceneIndex")]
    [SerializeField] private int mapSceneIndex = 1;
    [SerializeField] private int battleSceneIndex = 2;
    private bool isLoading;
    private System.Action startNewGame;
    private SceneMenuActions sharedLoader;
    public bool IsLoading => sharedLoader != null ? sharedLoader.IsLoading : isLoading;
    public string MapScenePath => SceneUtility.GetScenePathByBuildIndex(mapSceneIndex);
    public string BattleScenePath => SceneUtility.GetScenePathByBuildIndex(battleSceneIndex);

    //메뉴와 전투 이동의 공통 로더 주입
    public void InjectSceneLoader(SceneMenuActions loader)
    {
        if (loader == null || loader == this)
            throw new System.ArgumentException("다른 공통 씬 로더를 연결해 주세요", nameof(loader));
        sharedLoader = loader;
        loader.titleSceneIndex = titleSceneIndex;
        loader.mapSceneIndex = mapSceneIndex;
        loader.battleSceneIndex = battleSceneIndex;
    }

    //새 게임 데이터 초기화 기능 주입
    public void InjectNewGameAction(System.Action resetAction)
    {
        if (resetAction == null)
            throw new System.ArgumentNullException(nameof(resetAction));
        startNewGame = resetAction;
    }

    //시작 버튼의 게임 씬 이동
    public void StartGame()
    {
        LoadScene(mapSceneIndex, true);
    }

    //타이틀 버튼의 타이틀 씬 이동
    public void ReturnToTitle()
    {
        LoadScene(titleSceneIndex);
    }

    //빌드 씬 확인과 새 게임 초기화 및 중복 이동 차단
    private void LoadScene(int sceneIndex, bool newGame = false)
    {
        if (IsLoading)
            return;
        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings ||
            !Application.CanStreamedLevelBeLoaded(sceneIndex))
        {
            GameLogger.LogError($"[메뉴] 빌드 씬 목록의 {sceneIndex}번 씬을 확인해 주세요", this);
            return;
        }

        if (newGame && startNewGame == null)
        {
            GameLogger.LogError("[메뉴] 새 게임 초기화 기능을 연결해 주세요", this);
            return;
        }
        TryLoadScene(SceneUtility.GetScenePathByBuildIndex(sceneIndex), newGame ? startNewGame : null);
    }

    //빌드 목록의 이동 대상 확인
    public bool CanLoad(string path)
    {
        if (!string.IsNullOrEmpty(path) && Application.CanStreamedLevelBeLoaded(path)) return true;
        GameLogger.LogError($"[씬 이동] 빌드 씬 목록과 경로를 확인해 주세요: {path}", this);
        return false;
    }

    //메뉴와 전투 및 맵의 공통 비동기 이동 처리
    public bool TryLoadScene(string path, System.Action beforeActivation = null)
    {
        if (sharedLoader != null) return sharedLoader.TryLoadScene(path, beforeActivation);
        if (IsLoading || !CanLoad(path)) return false;
        isLoading = true;
        Time.timeScale = 1f;
        AsyncOperation operation = null;
        try
        {
            operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
            if (operation == null)
            {
                isLoading = false;
                return false;
            }
            operation.allowSceneActivation = false;
            operation.completed += OnSceneLoaded;
            beforeActivation?.Invoke();
            return true;
        }
        catch (System.Exception exception)
        {
            if (operation == null) isLoading = false;
            GameLogger.LogException(exception, this);
            return false;
        }
        finally
        {
            if (operation != null) operation.allowSceneActivation = true;
        }
    }

    //씬 로드 완료 후 공통 입력 잠금 해제
    private void OnSceneLoaded(AsyncOperation operation)
    {
        operation.completed -= OnSceneLoaded;
        isLoading = false;
    }

    //실행 환경에 따른 게임 종료와 에디터 재생 중지
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
