using System;
using UnityEngine;

//유적 탐사의 단계별 확률과 피해 및 성공 보상 설정
[CreateAssetMenu(menuName = "Game/Events/Ruins", fileName = "RuinsEvent")]
public class RuinsEventDefinition : MapEventDefinition
{
    [Serializable]
    public struct Attempt
    {
        [Range(0, 100)] public int successPercent;
        [Min(0)] public int failureDamage;
    }

    [SerializeField, Min(1)] private int rewardCoins = 100;
    [SerializeField, Range(1, 100)] private int retrySuccessIncrease = 10;
    [SerializeField] private Attempt[] attempts;

    //원본 설정 변경 없는 유적 방문 상태 생성
    public override MapEventVisit CreateVisit(RunProgressModel run, CoinModel coins, OwnedDeckModel deck, Func<float> random)
    {
        if (run == null || run.Health == null || coins == null || random == null)
            throw new ArgumentException("유적 이벤트 의존성을 확인해 주세요");
        if (rewardCoins <= 0 || attempts == null || attempts.Length == 0)
            throw new InvalidOperationException("유적 보상과 탐사 단계를 설정해 주세요");
        foreach (var attempt in attempts)
            if (attempt.successPercent < 0 || attempt.successPercent > 100 || attempt.failureDamage < 0)
                throw new InvalidOperationException("유적 확률과 피해 범위를 확인해 주세요");
        return new RuinsEventVisit(run, coins, random, (Attempt[])attempts.Clone(), rewardCoins, Mathf.Clamp(retrySuccessIncrease, 1, 100));
    }
}

//유적 재도전 횟수와 일회 보상 및 체력 손실 관리
public class RuinsEventVisit : MapEventVisit
{
    private readonly RunProgressModel run;
    private readonly CoinModel coins;
    private readonly Func<float> random;
    private readonly RuinsEventDefinition.Attempt[] attempts;
    private readonly int reward;
    private readonly int retrySuccessIncrease;
    private int attemptIndex;
    private bool succeeded;
    private bool processing;
    private string result = "유적에서 보물을 찾아보세요";
    public override int ChoiceCount => 1;
    public override string Status
    {
        get
        {
            if (succeeded || HasLeft) return result;
            var next = GetAttempt();
            return $"{result}\n\n{attemptIndex + 1}차 탐사\n성공 확률 {next.successPercent}%\n실패 시 HP -{next.failureDamage}\n성공 보상 {reward}코인" +
                (run.Health.CurrentHealth <= next.failureDamage ? "\n체력이 부족하여 탐사할 수 없습니다" : "");
        }
    }

    //유적 방문의 게임 상태와 확률 공급자 및 설정 보관
    public RuinsEventVisit(RunProgressModel run, CoinModel coins, Func<float> random, RuinsEventDefinition.Attempt[] attempts, int reward, int retrySuccessIncrease = 10)
    {
        this.run = run;
        this.coins = coins;
        this.random = random;
        this.attempts = attempts;
        this.reward = reward;
        this.retrySuccessIncrease = retrySuccessIncrease;
    }

    //설정 단계 이후 마지막 피해 유지와 성공 확률 상한 적용
    private RuinsEventDefinition.Attempt GetAttempt()
    {
        if (attemptIndex < attempts.Length) return attempts[attemptIndex];
        var next = attempts[attempts.Length - 1];
        next.successPercent = (int)Math.Min(100L, next.successPercent +
            (long)(attemptIndex - attempts.Length + 1) * retrySuccessIncrease);
        return next;
    }

    //시도 결과에 따른 탐사 버튼 문구 조회
    public override string GetChoiceLabel(int index) => succeeded ? "탐사 성공" : attemptIndex == 0 ? "탐사하기" : "다시 탐사";

    //종료와 체력 부족 및 중복 입력 차단
    public override bool CanChoose(int index) => index == 0 && !HasLeft && !processing && !succeeded &&
        run.Health.CurrentHealth > GetAttempt().failureDamage && coins.Amount <= int.MaxValue - reward;

    //현재 시도 확률 판정과 일회 보상 또는 누적 체력 손실 적용
    public override void Choose(int index)
    {
        if (!CanChoose(index)) return;
        processing = true;
        try
        {
            var current = GetAttempt();
            attemptIndex++;
            if (current.successPercent >= 100 || random() < current.successPercent / 100f)
            {
                succeeded = true;
                coins.TryAdd(reward);
                result = $"탐사 성공! {reward}코인을 획득했습니다";
            }
            else
            {
                run.Health.LoseHealth(current.failureDamage);
                result = $"탐사 실패! HP가 {current.failureDamage} 감소했습니다";
            }
        }
        finally { processing = false; }
    }
}
