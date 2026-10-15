using TMPro;
using UnityEngine;

//전투 외 화면의 세션 플레이어 체력 표시
public class PlayerHealthView : MonoBehaviour
{
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private UnityEngine.UI.Image healthFill;
    private CombatantState health;

    //세션 체력 연결과 이전 구독 해제
    public void InjectHealth(CombatantState source)
    {
        if (health != null) health.Changed -= Refresh;
        health = source;
        if (isActiveAndEnabled && health != null) health.Changed += Refresh;
        Refresh();
    }

    //화면 활성화 시 세션 체력 구독과 표시 갱신
    private void OnEnable()
    {
        if (health != null)
        {
            health.Changed -= Refresh;
            health.Changed += Refresh;
        }
        Refresh();
    }

    //체력 변경을 공통 텍스트와 게이지에 반영
    private void Refresh()
    {
        if (healthText != null)
            healthText.text = health != null ? $"HP {health.CurrentHealth} / {health.MaxHealth}" : "HP —";
        if (healthFill != null)
            healthFill.fillAmount = health != null ? (float)health.CurrentHealth / health.MaxHealth : 0f;
    }

    //화면 비활성화 시 세션 체력 구독 해제
    private void OnDisable()
    {
        if (health != null) health.Changed -= Refresh;
    }
}
