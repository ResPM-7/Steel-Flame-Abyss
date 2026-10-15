using TMPro;
using UnityEngine;
using UnityEngine.UI;

//공통 코인 표시에 전투 승리 보상 추가
public class BattleCoinInstaller : CoinInstaller
{
    [SerializeField] private BattleParticipants participants;
    [SerializeField] private Button rewardButton;
    [SerializeField] private TMP_Text rewardText;
    private CoinReward reward;
    private EnemyBattleState rewardEnemy;
    private readonly System.Random random = new System.Random();

    //공통 화면과 전투 보상 연결 확인
    protected override bool ValidateBindings()
    {
        if (!base.ValidateBindings()) return false;
        if (participants != null && rewardButton != null) return true;
        Debug.LogError("[전투 코인] Participants와 Reward Button을 연결해 주세요", this);
        return false;
    }

    //전투 결과와 보상 버튼 구독
    protected override void BindAdditional()
    {
        participants.ResultChanged += RefreshReward;
        rewardButton.onClick.AddListener(ClaimReward);
        RefreshReward(participants.Result);
    }

    //승리한 적의 보상 범위에서 일회 지급 금액 결정
    private void RefreshReward(BattleResult result)
    {
        if (rewardEnemy != participants.Enemy)
        {
            rewardEnemy = participants.Enemy;
            reward = null;
        }
        if (result == BattleResult.Victory && rewardEnemy != null && reward == null)
        {
            int min = Mathf.Max(0, rewardEnemy.Data.MinGold);
            int max = Mathf.Max(min, rewardEnemy.Data.MaxGold);
            int amount = min + (int)(random.NextDouble() * ((long)max - min + 1));
            reward = new CoinReward(amount);
        }
        rewardButton.interactable = result == BattleResult.Victory && reward != null &&
            !reward.IsClaimed && reward.Amount > 0;
        if (rewardText != null)
            rewardText.text = reward == null ? "Coin" :
                reward.IsClaimed ? "수령 완료" : $"Coin +{reward.Amount}";
    }

    //승리 보상 요청 확인과 보유 코인 추가
    private void ClaimReward()
    {
        if (participants.Result != BattleResult.Victory || reward == null) return;
        reward.TryClaim(Model);
        RefreshReward(participants.Result);
    }

    //전투 결과와 버튼 및 공통 코인 구독 해제
    protected override void ReleaseBindings()
    {
        if (participants != null) participants.ResultChanged -= RefreshReward;
        if (rewardButton != null) rewardButton.onClick.RemoveListener(ClaimReward);
        base.ReleaseBindings();
    }
}
