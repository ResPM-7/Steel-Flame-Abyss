using TMPro;
using UnityEngine;
using UnityEngine.UI;

//코인 화면과 전투 보상 의존성 연결
public class CoinInstaller : MonoBehaviour
{
    [SerializeField] private CoinView coinView;
    [SerializeField] private BattleParticipants participants;
    [SerializeField] private Button rewardButton;
    [SerializeField] private TMP_Text rewardText;

    private CoinModel model;
    private CoinPresenter presenter;
    private CoinReward reward;
    private EnemyBattleState rewardEnemy;
    private readonly System.Random random = new System.Random();

    //세션 코인 화면 연결과 전투 씬의 선택적 보상 연결
    public void Inject(CoinModel coinModel)
    {
        if (coinModel == null)
            throw new System.ArgumentNullException(nameof(coinModel));
        if (model == coinModel)
            return;
        if (coinView == null || !coinView.IsConfigured || (participants == null) != (rewardButton == null))
        {
            Debug.LogError("[코인] Coin View와 Coin Text 및 참가자와 보상 버튼을 연결해 주세요", this);
            return;
        }

        ReleaseBindings();
        model = coinModel;
        presenter = new CoinPresenter(model, coinView);
        if (participants == null) return;
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

        bool canClaim = result == BattleResult.Victory && reward != null &&
            !reward.IsClaimed && reward.Amount > 0;
        rewardButton.interactable = canClaim;
        if (rewardText != null)
            rewardText.text = reward == null ? "Coin" :
                reward.IsClaimed ? "수령 완료" : $"Coin +{reward.Amount}";
    }

    //승리 보상 요청 확인과 보유 코인 추가
    private void ClaimReward()
    {
        if (participants.Result != BattleResult.Victory || reward == null)
            return;

        reward.TryClaim(model);
        RefreshReward(participants.Result);
    }

    //씬 제거 시 화면과 보상 이벤트 연결 해제
    private void OnDestroy()
    {
        ReleaseBindings();
    }

    //기존 코인 변경과 전투 결과 및 버튼 구독 해제
    private void ReleaseBindings()
    {
        presenter?.Dispose();
        presenter = null;
        if (participants != null)
            participants.ResultChanged -= RefreshReward;
        if (rewardButton != null)
            rewardButton.onClick.RemoveListener(ClaimReward);
    }
}
