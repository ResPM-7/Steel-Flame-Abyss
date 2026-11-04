using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

//씬 공용 피해 텍스트 풀 준비와 표시 요청 처리
public class DamageTextSpawner : MonoBehaviour
{
    [SerializeField] private ObjectPoolManager objectPool;
    [SerializeField] private string poolName = "DamageText";
    [SerializeField] private Color damageColor = Color.white;
    [SerializeField] private Color burnColor = new Color(1f, 0.45f, 0.1f);
    [SerializeField] private Color blockColor = new Color(0.6f, 0.8f, 1f);
    [SerializeField, Min(0f)] private float horizontalSpread = 20f;

    //첫 피해 이전 풀 로드 시작
    private void Start() => PrepareAsync().Forget();

    //씬 종료 시 대기 취소와 로드 실패 알림
    private async UniTask<bool> PrepareAsync()
    {
        if (objectPool == null) return false;
        try
        {
            bool ready = await objectPool.EnsurePoolReadyAsync(poolName).AsUniTask()
                .AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
            if (!ready) GameLogger.LogError($"[피해 텍스트] 풀 이름과 Addressable 연결 확인: {poolName}", this);
            return ready;
        }
        catch (OperationCanceledException) { return false; }
        catch (Exception error) { if (this != null) GameLogger.LogException(error, this); return false; }
    }

    //유닛 제거 후에도 표시 가능한 화면 위치 저장
    public void Show(RectTransform anchor, DamageResult result)
    {
        if (anchor == null || !isActiveAndEnabled) return;
        var canvas = anchor.GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, anchor.position);
        point.x += UnityEngine.Random.Range(-horizontalSpread, horizontalSpread);
        ShowAsync(point, result).Forget();
    }

    //풀 준비 완료 후 숫자 표시, 초기 로딩 중 요청도 유지
    private async UniTask ShowAsync(Vector2 point, DamageResult result)
    {
        if (!await PrepareAsync() || this == null || !isActiveAndEnabled) return;
        var instance = objectPool.GetObject(poolName);
        if (instance == null) return;
        if (!instance.TryGetComponent<DamageTextView>(out var view))
        {
            objectPool.RequestReturn(poolName, instance);
            GameLogger.LogError("[피해 텍스트] 풀 프리팹에 DamageTextView 추가 필요", this);
            return;
        }
        bool blocked = result.HealthLost == 0;
        view.Show(objectPool, poolName, point, blocked ? "방어" : result.HealthLost.ToString(),
            blocked ? blockColor : result.Kind == DamageKind.Burn ? burnColor : damageColor);
    }
}
