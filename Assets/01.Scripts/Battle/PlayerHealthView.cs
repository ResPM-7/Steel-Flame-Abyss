using TMPro;
using UnityEngine;

//전투 외 화면의 공통 플레이어 체력 표시
public class PlayerHealthView : MonoBehaviour
{
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private UnityEngine.UI.Image healthFill;
    private BattleParticipants participants;
    private CombatantState health;

    //플레이어 조회 기능 주입과 체력 표시 연결
    public void Inject(BattleParticipants source)
    {
        if (source == null)
            throw new System.ArgumentNullException(nameof(source));
        Unsubscribe();
        participants = source;
        if (isActiveAndEnabled)
            Subscribe();
    }

    //공통 HUD 활성화 시 최신 플레이어 체력 연결
    private void OnEnable()
    {
        Subscribe();
    }

    //전투 결과 구독과 최신 체력 연결
    private void Subscribe()
    {
        if (participants == null)
            return;
        participants.ResultChanged -= OnBattleResult;
        participants.ResultChanged += OnBattleResult;
        BindHealth();
    }

    //전투 참가자 변경에 따른 체력 연결 갱신
    private void OnBattleResult(BattleResult result)
    {
        BindHealth();
    }

    //기존 체력 구독 해제와 현재 플레이어 체력 구독
    private void BindHealth()
    {
        if (health != null)
            health.Changed -= Refresh;
        health = participants.Player != null ? participants.Player.Stats : null;
        if (health != null)
            health.Changed += Refresh;
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

    //비활성화 시 체력과 전투 결과 구독 해제
    private void OnDisable()
    {
        Unsubscribe();
    }

    //기존 이벤트 연결 해제
    private void Unsubscribe()
    {
        if (participants != null)
            participants.ResultChanged -= OnBattleResult;
        if (health != null)
            health.Changed -= Refresh;
        health = null;
    }
}
