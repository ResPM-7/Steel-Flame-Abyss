using UnityEngine;
using UnityEngine.SceneManagement;

//메뉴 버튼의 게임 시작과 타이틀 이동 및 종료 처리
public class SceneMenuActions : MonoBehaviour
{
    [SerializeField] private int titleSceneIndex = 0;
    [SerializeField] private int gameSceneIndex = 1;
    private bool isLoading;
    private System.Action startNewGame;

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
        LoadScene(gameSceneIndex, true);
    }

    //타이틀 버튼의 타이틀 씬 이동
    public void ReturnToTitle()
    {
        LoadScene(titleSceneIndex);
    }

    //빌드 씬 확인과 새 게임 초기화 및 중복 이동 차단
    private void LoadScene(int sceneIndex, bool newGame = false)
    {
        if (isLoading)
            return;
        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings ||
            !Application.CanStreamedLevelBeLoaded(sceneIndex))
        {
            Debug.LogError($"[메뉴] 빌드 씬 목록의 {sceneIndex}번 씬을 확인해 주세요", this);
            return;
        }

        isLoading = true;
        if (newGame)
            startNewGame?.Invoke();
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(sceneIndex);
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
