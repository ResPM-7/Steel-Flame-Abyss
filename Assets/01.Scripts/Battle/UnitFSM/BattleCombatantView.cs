using TMPro;
using UnityEngine;
using UnityEngine.UI;

//플레이어와 적 공용 표시 컴포넌트 상태가 바뀔 때만 UI를 갱신
public class BattleCombatantView : MonoBehaviour
{
    [SerializeField] private Image portrait;
    [Tooltip("플레이어 전용 RenderTexture 표시, 적은 비워 두기")]
    [SerializeField] private BattlePlayerRenderView playerRender;
    [SerializeField] private Image healthFill;

    [SerializeField] private TMP_Text healthLabel;
    [Tooltip("버프·디버프 줄의 위치와 폰트 기준, 기존 텍스트는 숨김")]
    [SerializeField] private TMP_Text statusLabel;
    [Tooltip("플레이어와 적에 같은 공용 아이콘 SO 연결")]
    [SerializeField] private BattleStatusIcons statusIcons;
    [Header("방어도 표시")]
    [SerializeField] private GameObject shieldRoot;
    [SerializeField] private TMP_Text shieldLabel;
    [Tooltip("적의 다음 행동 표시용 DetailText, 플레이어는 선택 연결")]
    [SerializeField] private TMP_Text detailLabel;

    [SerializeField] private Color placeholderColor = new Color(0.3f, 0.6f, 0.85f);

    [Header("카드 타겟팅")]
    [SerializeField] private RectTransform targetAnchor;
    [SerializeField] private RectTransform targetArea;
    [Tooltip("선택 표시 전용 이미지, Raycast Target 해제 권장")]
    [SerializeField] private UnityEngine.UI.Graphic targetHighlight;
    public RectTransform TargetAnchor => targetAnchor;

    //선택 영역 미지정 시 CombatantInfo 전체 범위 사용
    public bool ContainsScreenPoint(Vector2 screenPosition)
    {
        var area = targetArea != null ? targetArea : transform as RectTransform;
        var canvas = area.GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(area, screenPosition, camera);
    }

    //선택 강조 전용 그래픽 표시
    public void SetTargetHighlight(bool selected)
    {
        if (targetHighlight != null)
        {
            targetHighlight.raycastTarget = false;
            targetHighlight.enabled = selected;
        }
    }

    private CombatantState state;
    private string detail = string.Empty;
    private Color aliveColor;
    private BattleStatusBarView statusBar;
    private BattleUnitPortraitView unitPortrait;
    public CombatantState State => state;

    //전투 상태와 표시 데이터 연결 및 초상화 대상 감지 활성화
    public void Bind(string displayName, Sprite sprite, CombatantState value, GameObject battlePrefab = null, bool faceLeft = false)
    {
        if (state != null) state.Changed -= Refresh;

        SetTargetHighlight(false);
        state = value;
        if (portrait != null) portrait.sprite = sprite;
        aliveColor = sprite != null ? Color.white : placeholderColor;

        //전투 표시 객체 한 번 생성, 재연결 시 기존 객체 정리
        if (playerRender != null)
        {
            if (unitPortrait != null) unitPortrait.Bind(portrait, null, null);
            playerRender.Bind(battlePrefab, state);
            if (portrait != null) portrait.enabled = false;
        }
        else
        {
            if (unitPortrait == null) unitPortrait = gameObject.AddComponent<BattleUnitPortraitView>();
            unitPortrait.Bind(portrait, battlePrefab, state, faceLeft);
        }

        //인스펙터에 추가한 피해 연출 컴포넌트에 전투 상태 전달
        if (TryGetComponent<BattleDamageFeedback>(out var feedback)) feedback.Bind(state);

        if (state != null && isActiveAndEnabled) state.Changed += Refresh;
        Refresh();
    }

    //캐릭터 설명이나 적의 다음 행동 표시를 갱신
    public void SetDetail(string value)
    {
        detail = value ?? string.Empty;
        Refresh();
    }

    //활성화 시 상태 변경을 구독하고 현재 UI를 갱신
    private void OnEnable()
    {
        if (state != null) state.Changed += Refresh;
        Refresh();
    }

    //비활성화 시 상태 변경 구독을 해제
    private void OnDisable()
    {
        SetTargetHighlight(false);
        if (state != null) state.Changed -= Refresh;
        if (statusBar != null) statusBar.HideTooltip();
    }

    //체력 능력치 생존 여부를 UI에 반영
    private void Refresh()
    {
        //전달받은 다음 행동 문자열을 실제 UI에 반영
        if (detailLabel != null) detailLabel.text = detail;
        if (state == null) return;
        healthFill.fillAmount = (float)state.CurrentHealth / state.MaxHealth;

        healthLabel.text = $"HP {state.CurrentHealth} / {state.MaxHealth}";
        //방어도가 남아 있는 생존 유닛만 방패 표시
        if (shieldRoot != null) shieldRoot.SetActive(state.IsAlive && state.Block > 0);
        if (shieldLabel != null) shieldLabel.text = state.Block.ToString();

        //기존 텍스트는 지우고 같은 위치에 버프·디버프 아이콘 표시
        if (statusLabel != null)
        {
            if (statusBar == null) statusBar = BattleStatusBarView.Create(statusLabel, statusIcons);
            statusLabel.text = string.Empty;
            statusLabel.enabled = false;
        }
        if (statusBar != null) statusBar.Refresh(state);
        if (portrait != null && playerRender == null && (unitPortrait == null || !unitPortrait.HasVisual))
            portrait.color = state.IsAlive ? aliveColor : new Color(0.3f, 0.3f, 0.3f, 0.5f);
    }
}
