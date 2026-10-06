using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SteelFlameAbyss.Battle
{
    /// <summary>플레이어와 적 공용 표시 컴포넌트입니다. 상태가 바뀔 때만 UI를 갱신합니다.</summary>
    public sealed class BattleCombatantView : MonoBehaviour
    {
        [SerializeField] private Image portrait;
        [SerializeField] private Image healthFill;

        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text healthLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text detailLabel;

        [SerializeField] private Color placeholderColor = new Color(0.3f, 0.6f, 0.85f);

        private CombatantState state;
        private string detail = string.Empty;
        private Color aliveColor;
        public CombatantState State => state;

        public void Bind(string displayName, Sprite sprite, CombatantState value)
        {
            if (state != null) state.Changed -= Refresh;
            state = value;
            nameLabel.text = displayName;
            portrait.sprite = sprite;
            portrait.preserveAspect = true;
            aliveColor = sprite != null ? Color.white : placeholderColor;
            if (state != null && isActiveAndEnabled) state.Changed += Refresh;
            Refresh();
        }

        public void SetDetail(string value)
        {
            detail = value ?? string.Empty;
            Refresh();
        }

        private void OnEnable()
        {
            if (state != null) state.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (state != null) state.Changed -= Refresh;
        }

        private void Refresh()
        {
            if (state == null) return;
            healthFill.fillAmount = (float)state.CurrentHealth / state.MaxHealth;
            healthLabel.text = $"HP {state.CurrentHealth} / {state.MaxHealth}";
            statusLabel.text = $"방어 {state.Block}   힘 {state.Strength}";
            if (state.Weak > 0) statusLabel.text += $"   약화 {state.Weak}";
            if (state.Vulnerable > 0) statusLabel.text += $"   취약 {state.Vulnerable}";
            portrait.color = state.IsAlive ? aliveColor : new Color(0.3f, 0.3f, 0.3f, 0.5f);
            detailLabel.text = state.IsAlive ? detail : "전투 불능";
        }
    }
}
