using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

//씬 비동기 로드와 플레이 준비 완료를 하나의 전환으로 관리
public class SceneTransitionController : MonoBehaviour
{
    [SerializeField] private float preparationTimeout = 45f;
    private LoadingScreenView screen;
    public static bool BlocksGameplay { get; private set; }
    public bool IsLoading { get; private set; }

    //도메인 재로드 생략 시 입력 잠금 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() => BlocksGameplay = false;

    //요청 검증과 중복 전환 차단 및 데이터 준비
    public bool TryLoad(string path, Action beforeActivation, string titlePath)
    {
        if (IsLoading || !Application.CanStreamedLevelBeLoaded(path)) return false;
        IsLoading = true;
        try { beforeActivation?.Invoke(); }
        catch (Exception error) { IsLoading = false; GameLogger.LogException(error, this); return false; }
        BlocksGameplay = true;
        Time.timeScale = 1f;
        if (screen == null) screen = LoadingScreenView.Create(transform);
        screen.Show("씬을 불러오는 중…");
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        LoadAsync(path, titlePath, destroyCancellationToken).Forget();
        return true;
    }

    //로딩 화면 한 프레임 표시 후 씬 로드와 각 준비 작업 대기
    private async UniTask LoadAsync(string path, string titlePath, CancellationToken cancellationToken)
    {
        try
        {
            await UniTask.NextFrame(cancellationToken);
            var operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
            if (operation == null) throw new InvalidOperationException("씬 로드 요청 실패");
            await operation.ToUniTask(cancellationToken: cancellationToken);
            screen.Show("게임을 준비하는 중…");
            //sceneLoaded 데이터 주입과 Start 초기화 이후 준비 조건 확인
            await UniTask.NextFrame(cancellationToken);

            //세션 종료 또는 제한 시간 초과 시 준비 대기 취소
            using var preparationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            using var timeout = preparationCancellation.CancelAfterSlim(
                TimeSpan.FromSeconds(preparationTimeout), DelayType.Realtime);
            var tasks = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .OfType<IScenePreparation>()
                .Select(item => item.PrepareSceneAsync(preparationCancellation.Token)).ToArray();
            await UniTask.WhenAll(tasks).AttachExternalCancellation(preparationCancellation.Token);

            Canvas.ForceUpdateCanvases();
            await UniTask.NextFrame(cancellationToken);
            screen.Hide();
            BlocksGameplay = false;
            IsLoading = false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            //세션 제거 이후 UI 접근 생략
        }
        catch (OperationCanceledException)
        {
            GameLogger.LogError("[씬 준비] 준비 취소 또는 제한 시간 초과", this);
            Fail(titlePath);
        }
        catch (Exception error)
        {
            GameLogger.LogException(error, this);
            if (!cancellationToken.IsCancellationRequested) Fail(titlePath);
        }
    }

    //실패 화면 유지와 타이틀 복귀만 허용
    private void Fail(string titlePath)
    {
        BlocksGameplay = true;
        IsLoading = true;
        screen.ShowFailure(() =>
        {
            IsLoading = false;
            if (!TryLoad(titlePath, null, titlePath)) Fail(titlePath);
        });
    }

    //세션 종료 시 전역 잠금 해제
    private void OnDestroy() => BlocksGameplay = false;
}
