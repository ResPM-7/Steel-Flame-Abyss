using System;
using UnityEngine;

//이벤트별 설정과 방문 상태 생성 계약
public abstract class MapEventDefinition : ScriptableObject
{
    [SerializeField, Min(1)] private int eventId = 1;
    [SerializeField] private string displayName = "새 이벤트";
    public int EventId => eventId;
    public string DisplayName => displayName;

    //주입된 게임 데이터로 독립 이벤트 방문 생성
    public abstract MapEventVisit CreateVisit(RunProgressModel run, CoinModel coins, OwnedDeckModel deck, Func<float> random);
}

//공통 이벤트 화면에 제공하는 방문 규칙
public abstract class MapEventVisit
{
    public bool HasLeft { get; private set; }
    public abstract string Status { get; }
    public abstract int ChoiceCount { get; }
    //선택지 표시 이름 조회
    public abstract string GetChoiceLabel(int index);
    //선택지 실행 가능 상태 조회
    public abstract bool CanChoose(int index);
    //선택한 이벤트 규칙 실행
    public abstract void Choose(int index);
    //이벤트 방문 종료와 재실행 차단
    public void Leave() { HasLeft = true; }
}
