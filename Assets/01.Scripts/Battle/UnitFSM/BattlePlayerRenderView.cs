using UnityEngine;

//선택한 SPUM 캐릭터를 전용 카메라와 RawImage로 표시
public class BattlePlayerRenderView : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.RawImage portrait;
    [SerializeField] private Camera renderCamera;
    [SerializeField] private Transform characterRoot;
    [SerializeField] private RenderTexture renderTexture;

    private GameObject instance;
    private SPUM_Prefabs spum;
    private Animator animator;
    private AnimatorOverrideController ownedController;
    private CombatantState state;
    private bool dead;
    private bool subscribed;
    public bool HasVisual => instance != null;

    //기존 캐릭터 정리 후 선택한 전투 프리팹 생성
    public void Bind(GameObject prefab, CombatantState value)
    {
        Clear();
        if (prefab == null) return;
        if (portrait == null || renderCamera == null || characterRoot == null || renderTexture == null)
        {
            GameLogger.LogError("[플레이어 표시] RawImage, Camera, Character Root, RenderTexture 연결 필요", this);
            return;
        }

        state = value;
        instance = Instantiate(prefab, characterRoot, false);
        instance.name = prefab.name + "_BattleVisual";
        instance.transform.localPosition = Vector3.zero;
        //생성 위치의 레이어를 모든 파츠에 적용
        foreach (var child in instance.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = characterRoot.gameObject.layer;
        foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            renderer.forceRenderingOff = false;

        spum = instance.GetComponentInChildren<SPUM_Prefabs>(true);
        animator = spum != null ? spum._anim : instance.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (spum != null)
            {
                spum.OverrideControllerInit();
                ownedController = spum.OverrideController;
            }
            Play(PlayerState.IDLE);
        }

        renderCamera.targetTexture = renderTexture;
        portrait.texture = renderTexture;
        portrait.color = Color.white;
        portrait.raycastTarget = false;
        portrait.enabled = isActiveAndEnabled;
        renderCamera.enabled = isActiveAndEnabled;
        Subscribe();
        RefreshDeath();
    }

    //클립 목록이 있으면 적용, 비어 있으면 기본 컨트롤러 클립 사용
    private void Play(PlayerState action)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        string key = action.ToString();
        if (spum != null && ownedController != null &&
            spum.StateAnimationPairs.TryGetValue(key, out var clips) &&
            clips.Count > 0 && clips[0] != null)
            ownedController[key] = clips[0];

        foreach (var parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool)
            {
                if (parameter.name == "1_Move" || parameter.name == "5_Debuff")
                    animator.SetBool(parameter.nameHash, false);
                else if (parameter.name == "isDeath")
                    animator.SetBool(parameter.nameHash, action == PlayerState.DEATH);
            }
            else if (parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(parameter.nameHash);
                if (parameter.name.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    animator.SetTrigger(parameter.nameHash);
            }
        }
        if (action == PlayerState.IDLE && animator.HasState(0, Animator.StringToHash("Base Layer.IDLE")))
            animator.Play("Base Layer.IDLE", 0, 0f);
    }

    //생존 중 공격과 피격 재생
    private void PlayAttack() { if (state.IsAlive && !dead) Play(PlayerState.ATTACK); }
    private void PlayHit() { if (state.IsAlive && !dead) Play(PlayerState.DAMAGED); }

    //사망 애니메이션 한 번만 재생
    private void RefreshDeath()
    {
        if (state == null || state.IsAlive || dead) return;
        dead = true;
        Play(PlayerState.DEATH);
    }

    //중복 없는 전투 이벤트 연결
    private void Subscribe()
    {
        if (subscribed || state == null || !isActiveAndEnabled) return;
        state.AttackPerformed += PlayAttack;
        state.Damaged += PlayHit;
        state.Changed += RefreshDeath;
        subscribed = true;
    }

    //전투 이벤트 연결 해제
    private void Unsubscribe()
    {
        if (!subscribed || state == null) return;
        state.AttackPerformed -= PlayAttack;
        state.Damaged -= PlayHit;
        state.Changed -= RefreshDeath;
        subscribed = false;
    }

    //화면 복귀 시 촬영과 상태 갱신
    private void OnEnable()
    {
        if (!HasVisual) return;
        renderCamera.enabled = true;
        portrait.enabled = true;
        Subscribe();
        RefreshDeath();
    }

    //숨겨진 화면의 촬영 중지
    private void OnDisable()
    {
        Unsubscribe();
        if (renderCamera != null) renderCamera.enabled = false;
        if (portrait != null) portrait.enabled = false;
    }

    //생성 객체만 정리, 프로젝트 RenderTexture 에셋 유지
    private void Clear()
    {
        Unsubscribe();
        if (renderCamera != null) renderCamera.enabled = false;
        if (portrait != null) { portrait.texture = null; portrait.enabled = false; }
        if (instance != null) { instance.SetActive(false); Destroy(instance); }
        if (ownedController != null) Destroy(ownedController);
        instance = null;
        ownedController = null;
        spum = null;
        animator = null;
        state = null;
        dead = false;
    }

    //씬 종료 시 생성 캐릭터와 이벤트 정리
    private void OnDestroy() => Clear();
}
