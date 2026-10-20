using UnityEngine;

//승리 팝업의 보상과 다음 버튼 참조 제공
public class BattleWinPopupView : MonoBehaviour
{
    [SerializeField] private CardRewardView rewardView;
    [SerializeField] private UnityEngine.UI.Button coinButton;
    [SerializeField] private TMPro.TMP_Text coinText;
    [SerializeField] private UnityEngine.UI.Button nextButton;
    public CardRewardView RewardView => rewardView;
    public UnityEngine.UI.Button CoinButton => coinButton;
    public TMPro.TMP_Text CoinText => coinText;
    public UnityEngine.UI.Button NextButton => nextButton;
}
