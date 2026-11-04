using DG.Tweening;
using UnityEngine;

//유닛별 피해 숫자 표시와 캐릭터 그림 흔들림
public class BattleDamageFeedback : MonoBehaviour
{
    [SerializeField] private RectTransform damageTextAnchor;
    [Tooltip("플레이어 RawImage 또는 적 EnemyImage, HP바와 판정 루트 제외")]
    [SerializeField] private RectTransform shakeTarget;
    [SerializeField, Min(0.01f)] private float shakeDuration = 0.15f;
    [SerializeField, Min(0f)] private float shakeStrength = 8f;
    private CombatantState state;
    private DamageTextSpawner spawner;
    private Tween shake;
    private Vector2 restingPosition;
    private bool shaking;
    private bool subscribed;

    //동적 생성된 적도 같은 씬의 텍스트 표시기 연결
    public void Bind(CombatantState value)
    {
        Unsubscribe();
        StopShake();
        state = value;
        spawner = FindFirstObjectByType<DamageTextSpawner>();
        Subscribe();
    }

    //실제 피해 결과 표시, HP 손실이 있을 때만 흔들림
    private void OnDamage(DamageResult result)
    {
        if (spawner != null) spawner.Show(damageTextAnchor, result);
        if (result.HealthLost <= 0 || shakeTarget == null) return;
        StopShake();
        restingPosition = shakeTarget.anchoredPosition;
        shaking = true;
        shake = shakeTarget.DOShakeAnchorPos(shakeDuration, new Vector2(shakeStrength, 0), 10, 0, false, true)
            .OnComplete(StopShake);
    }

    //연속 피격과 화면 종료 시 기준 위치 복구
    private void StopShake()
    {
        shake?.Kill();
        shake = null;
        if (shaking && shakeTarget != null) shakeTarget.anchoredPosition = restingPosition;
        shaking = false;
    }

    //활성 유닛만 피해 이벤트 구독
    private void Subscribe()
    {
        if (subscribed || state == null || !isActiveAndEnabled) return;
        state.DamageResolved += OnDamage;
        subscribed = true;
    }

    //재연결과 비활성화 시 구독 해제
    private void Unsubscribe()
    {
        if (subscribed && state != null) state.DamageResolved -= OnDamage;
        subscribed = false;
    }

    //화면 복귀 시 구독 재개
    private void OnEnable() => Subscribe();

    //화면 종료 시 연출 정리
    private void OnDisable() { Unsubscribe(); StopShake(); }
}
