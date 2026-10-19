using System;
using UnityEngine;

//전투 결과와 카드 보상 모델 및 화면 연결
public class CardRewardPresenter : IDisposable
{
    private readonly IGameDataProvider data;
    private readonly BattleParticipants participants;
    private readonly OwnedDeckModel deck;
    private readonly CardRewardView view;
    private readonly System.Random random = new System.Random();
    private EnemyBattleState enemy;
    private CardRewardModel reward;

    //카드 보상 의존성 저장과 화면 이벤트 연결
    public CardRewardPresenter(IGameDataProvider data, BattleParticipants participants, OwnedDeckModel deck, CardRewardView view)
    {
        if (data == null || participants == null || deck == null || view == null)
            throw new ArgumentNullException();
        this.data = data;
        this.participants = participants;
        this.deck = deck;
        this.view = view;
        participants.ResultChanged += Refresh;
        view.OpenRequested += Open;
        view.CloseRequested += Close;
        view.Selected += Select;
        Refresh(participants.Result);
    }

    //새 전투의 후보 초기화와 보상 가능 상태 갱신
    private void Refresh(BattleResult result)
    {
        if (enemy != participants.Enemy)
        {
            enemy = participants.Enemy;
            reward = null;
            view.Close();
        }
        if (result != BattleResult.Victory)
            view.Close();
        view.SetRewardState(result == BattleResult.Victory, reward != null && reward.IsClaimed);
    }

    //첫 열기에서 후보 추첨 후 기존 후보 재사용
    private void Open()
    {
        if (participants.Result != BattleResult.Victory || !data.IsReady || !deck.IsInitialized ||
            (reward != null && reward.IsClaimed))
            return;
        if (reward == null)
            reward = new CardRewardModel(data.Database.Cards, participants.Player.Data.Class, random);
        if (reward.Candidates.Count == 0)
        {
            GameLogger.LogWarning("[카드 보상] 선택 가능한 캐릭터 또는 공용 카드가 없습니다");
            return;
        }
        view.Show(reward.Candidates);
    }

    //수령 상태를 유지한 선택창 닫기
    private void Close() { view.Close(); }

    //보상 선택 검증과 보유 덱 추가 후 화면 종료
    private void Select(int index)
    {
        if (participants.Result != BattleResult.Victory || reward == null || !reward.TryClaim(index, deck))
            return;
        view.Close();
        view.SetRewardState(false, true);
        GameLogger.Log($"[카드 보상] {reward.Candidates[index].DisplayName} 추가 / 보유 덱 {deck.CardIds.Count}장");
    }

    //전투 결과와 화면 이벤트 구독 해제
    public void Dispose()
    {
        participants.ResultChanged -= Refresh;
        view.OpenRequested -= Open;
        view.CloseRequested -= Close;
        view.Selected -= Select;
    }
}
