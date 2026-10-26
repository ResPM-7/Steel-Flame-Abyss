using UnityEngine;

//전투당 생성한 유닛의 애니메이션을 Canvas 초상화에 표시
[DefaultExecutionOrder(10000)]
public class BattleUnitPortraitView : MonoBehaviour
{
    private GameObject instance;
    private SpriteRenderer spriteRenderer;
    private BattleUnitAnimationView animationView;
    private UnityEngine.UI.Image portrait;
    private UnityEngine.UI.Image animatedImage;
    private bool faceLeft;
    private CombatantState state;
    private bool dead;
    private bool subscribed;
    public bool HasVisual => instance != null;

    //표시 프리팹 생성과 전투 상태 연결
    public void Bind(UnityEngine.UI.Image target, GameObject prefab, CombatantState value, bool lookLeft = false)
    {
        Unsubscribe();
        if (instance != null)
        {
            instance.SetActive(false);
            Destroy(instance);
        }
        instance = null;
        if (animatedImage != null)
        {
            animatedImage.gameObject.SetActive(false);
            Destroy(animatedImage.gameObject);
        }
        animatedImage = null;
        spriteRenderer = null;
        animationView = null;
        portrait = target;
        state = value;
        dead = false;
        faceLeft = lookLeft;
        if (prefab == null || target == null) return;

        instance = Instantiate(prefab, target.transform, false);
        instance.name = prefab.name + "_Visual";
        animationView = instance.GetComponentInChildren<BattleUnitAnimationView>(true);
        spriteRenderer = instance.GetComponentInChildren<SpriteRenderer>(true);
        //월드 중복 렌더링만 차단, Animator와 SpriteResolver 갱신 유지
        foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            renderer.forceRenderingOff = true;
        foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        //기존 이미지의 드롭 판정 유지, 자식 이미지로 투명 여백 보정
        portrait.color = Color.clear;
        var imageObject = new GameObject("AnimatedPortrait", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        imageObject.transform.SetParent(target.transform, false);
        animatedImage = imageObject.GetComponent<UnityEngine.UI.Image>();
        animatedImage.raycastTarget = false;
        animatedImage.preserveAspect = true;
        var rect = animatedImage.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        if (isActiveAndEnabled) Subscribe();
        RefreshDeath();
        LateUpdate();
    }

    //SpriteResolver 처리 이후 프레임과 피격 색상 반영
    private void LateUpdate()
    {
        if (spriteRenderer == null || animatedImage == null || spriteRenderer.sprite == null) return;
        animatedImage.sprite = spriteRenderer.sprite;
        animatedImage.color = spriteRenderer.color;
        float width = portrait.rectTransform.rect.width * (animationView != null ? animationView.PortraitScale : 1f);
        var spriteRect = spriteRenderer.sprite.rect;
        animatedImage.rectTransform.sizeDelta = new Vector2(width, width * spriteRect.height / spriteRect.width);
        animatedImage.rectTransform.localScale = new Vector3(faceLeft ^ spriteRenderer.flipX ? -1f : 1f, spriteRenderer.flipY ? -1f : 1f, 1f);
    }

    //활성화 시 이벤트 연결
    private void OnEnable()
    {
        Subscribe();
        RefreshDeath();
    }

    //비활성화 시 이벤트 해제
    private void OnDisable() => Unsubscribe();

    //중복 없는 전투 이벤트 구독
    private void Subscribe()
    {
        if (subscribed || state == null || animationView == null) return;
        state.AttackPerformed += PlayAttack;
        state.Damaged += PlayHit;
        state.Changed += RefreshDeath;
        subscribed = true;
    }

    //이전 전투 상태 구독 해제
    private void Unsubscribe()
    {
        if (!subscribed || state == null) return;
        state.AttackPerformed -= PlayAttack;
        state.Damaged -= PlayHit;
        state.Changed -= RefreshDeath;
        subscribed = false;
    }

    //공격자 공통 행동 재생
    private void PlayAttack() => animationView.PlayAttack();

    //생존 대상의 피격 연출 재생
    private void PlayHit()
    {
        if (state.IsAlive) animationView.PlayHit();
    }

    //공격·화상·처형 사망을 한 번만 재생
    private void RefreshDeath()
    {
        if (state == null || state.IsAlive || dead || animationView == null) return;
        dead = true;
        animationView.PlayDead();
    }

    //씬 종료 또는 컴포넌트 제거 시 표시 객체 정리
    private void OnDestroy()
    {
        Unsubscribe();
        if (instance != null) Destroy(instance);
        if (animatedImage != null) Destroy(animatedImage.gameObject);
    }
}
