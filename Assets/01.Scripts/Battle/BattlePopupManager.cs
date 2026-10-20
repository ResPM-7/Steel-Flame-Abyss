using UnityEngine;

//전투 팝업의 최초 생성과 의존성 연결 및 재사용
public class BattlePopupManager : MonoBehaviour
{
    [SerializeField] private Transform popupRoot;
    [SerializeField] private CardListPanelView deckPrefab;
    [SerializeField] private BattleWinPopupView winPrefab;
    [SerializeField] private BattleDefeatPopupView defeatPrefab;
    [SerializeField] private BattleCoinInstaller coinInstaller;
    [SerializeField] private BattleSceneController sceneController;
    [SerializeField] private SceneMenuActions menu;
    private CardListPanelView deckView;
    private BattleWinPopupView winView;
    private BattleDefeatPopupView defeatView;
    private OwnedDeckModel ownedDeck;
    private IGameDataProvider data;
    private BattleDeckManager battleDeck;
    private BattleParticipants participants;
    private ObjectPoolManager pool;
    private CardRewardPresenter rewardPresenter;

    //전투 데이터 보관과 카드 풀 사전 준비
    public void Inject(OwnedDeckModel cards, IGameDataProvider provider, BattleDeckManager deck,
        BattleParticipants combatants, ObjectPoolManager objectPool)
    {
        ownedDeck = cards;
        data = provider;
        battleDeck = deck;
        participants = combatants;
        pool = objectPool;
        if (deckPrefab != null) _ = deckPrefab.PreparePoolAsync(pool);
    }

    //비활성 프리팹의 초기화 완료 후 인스턴스 보관
    private T Create<T>(T prefab) where T : Component
    {
        var instance = Instantiate(prefab, popupRoot, false);
        instance.name = prefab.name;
        instance.gameObject.SetActive(true);
        instance.gameObject.SetActive(false);
        return instance;
    }

    //덱 목록 최초 생성과 공통 데이터 주입
    private CardListPanelView GetDeck()
    {
        if (ownedDeck == null) return null;
        if (deckView == null)
        {
            deckView = Create(deckPrefab);
            deckView.Inject(ownedDeck, data, battleDeck, pool);
        }
        deckView.transform.SetAsLastSibling();
        return deckView;
    }

    //보유 덱 목록 표시
    public void ShowOwnedDeck() { GetDeck()?.ShowOwnedDeck(); }
    //뽑기 더미 목록 표시
    public void ShowDrawPile() { GetDeck()?.ShowDrawPile(); }
    //버린 더미 목록 표시
    public void ShowDiscardPile() { GetDeck()?.ShowDiscardPile(); }

    //전투 결과별 팝업 최초 생성과 표시
    public void ShowResult(BattleResult result)
    {
        if (deckView != null) deckView.Close();
        if (winView != null) winView.gameObject.SetActive(false);
        if (defeatView != null) defeatView.gameObject.SetActive(false);
        if (result == BattleResult.Victory)
        {
            if (winView == null)
            {
                winView = Create(winPrefab);
                coinInstaller.BindRewardView(winView.CoinButton, winView.CoinText);
                sceneController.BindNextButton(winView.NextButton);
                rewardPresenter = new CardRewardPresenter(data, participants, ownedDeck, winView.RewardView);
            }
            winView.transform.SetAsLastSibling();
            winView.gameObject.SetActive(true);
        }
        else if (result == BattleResult.Defeat)
        {
            if (defeatView == null)
            {
                defeatView = Create(defeatPrefab);
                defeatView.Inject(menu);
            }
            defeatView.transform.SetAsLastSibling();
            defeatView.gameObject.SetActive(true);
        }
    }

    //씬 종료 시 카드 보상 구독 해제
    private void OnDestroy() { rewardPresenter?.Dispose(); }
}
