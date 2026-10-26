using UnityEngine;

//입력 장치 없이 전투 코드에서 호출하는 유닛 애니메이션
[RequireComponent(typeof(Animator))]
public class BattleUnitAnimationView : MonoBehaviour
{
    [Tooltip("투명 여백이 포함된 스프라이트의 UI 표시 배율")]
    [Min(0.1f)] [SerializeField] private float portraitScale = 1f;
    public float PortraitScale => portraitScale;
    private Animator animator;
    private static readonly int Idle = Animator.StringToHash("Idle");
    private static readonly int Die = Animator.StringToHash("Die");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int Hit = Animator.StringToHash("Hit");

    //애니메이터 참조 저장
    private void Awake() => animator = GetComponent<Animator>();

    //풀 재사용 시 사망 상태와 남은 트리거 초기화
    private void OnEnable() => ResetForReuse();

    //새 전투 또는 풀 재사용을 위한 대기 상태 초기화
    public void ResetForReuse()
    {
        if (animator == null) animator = GetComponent<Animator>();
        animator.ResetTrigger(Attack);
        animator.ResetTrigger(Hit);
        animator.SetBool(Die, false);
        animator.SetBool(Idle, true);
        animator.Play("Base Layer.Idle", 0, 0f);
    }

    //생존 중 대기 상태로 복귀
    public void PlayIdle()
    {
        if (animator.GetBool(Die)) return;
        animator.ResetTrigger(Attack);
        animator.ResetTrigger(Hit);
        animator.SetBool(Idle, true);
        animator.Play("Base Layer.Idle", 0, 0f);
    }

    //생존 중 공격 재생 후 자동 대기 복귀
    public void PlayAttack()
    {
        if (animator.GetBool(Die)) return;
        animator.ResetTrigger(Hit);
        animator.SetTrigger(Attack);
    }

    //생존 중 피격 효과 재생 후 자동 대기 복귀
    public void PlayHit()
    {
        if (animator.GetBool(Die)) return;
        animator.ResetTrigger(Attack);
        animator.SetTrigger(Hit);
    }

    //사망 재생 후 마지막 프레임 유지
    public void PlayDead()
    {
        animator.ResetTrigger(Attack);
        animator.ResetTrigger(Hit);
        animator.SetBool(Idle, false);
        animator.SetBool(Die, true);
    }
}
