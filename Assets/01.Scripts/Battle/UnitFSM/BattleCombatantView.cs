using TMPro;
using UnityEngine;
using UnityEngine.UI;

//플레이어와 적 공용 표시 컴포넌트 상태가 바뀔 때만 UI를 갱신
public class BattleCombatantView : MonoBehaviour
{
    [SerializeField] private Image portrait;
    [SerializeField] private Image healthFill;

    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text healthLabel;
    [SerializeField] private TMP_Text statusLabel;
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
        nameLabel.text = displayName;
        portrait.sprite = sprite;
        aliveColor = sprite != null ? Color.white : placeholderColor;

        //전투 표시 객체 한 번 생성, 재연결 시 기존 객체 정리
        if (unitPortrait == null) unitPortrait = gameObject.AddComponent<BattleUnitPortraitView>();
        unitPortrait.Bind(portrait, battlePrefab, state, faceLeft);

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
        if (state == null) return;
        healthFill.fillAmount = (float)state.CurrentHealth / state.MaxHealth;

        healthLabel.text = $"HP {state.CurrentHealth} / {state.MaxHealth}";
        statusLabel.text = $"방어 {state.Block}   힘 {state.Strength}";

        //기존 능력치 아래에 상태 아이콘 생성 후 재사용
        if (statusBar == null) statusBar = BattleStatusBarView.Create(statusLabel);
        statusBar.Refresh(state);
        if (unitPortrait == null || !unitPortrait.HasVisual)
            portrait.color = state.IsAlive ? aliveColor : new Color(0.3f, 0.3f, 0.3f, 0.5f);
        detailLabel.text = state.IsAlive ? detail : "전투 불능";
    }
}
