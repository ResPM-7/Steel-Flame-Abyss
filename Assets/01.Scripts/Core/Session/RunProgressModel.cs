using System;

//씬과 분리된 체력과 맵 진행 및 전투 진입 정보 보관
public class RunProgressModel
{
    private readonly System.Collections.Generic.Dictionary<int, ShopModel> shopVisits = new System.Collections.Generic.Dictionary<int, ShopModel>();

    //씬 이동과 무관한 상점 상품 및 품절 상태 유지
    public ShopModel GetShopVisit(int nodeIndex, Func<ShopModel> create)
    {
        if (!shopVisits.TryGetValue(nodeIndex, out ShopModel shop))
        {
            shop = create();
            shopVisits.Add(nodeIndex, shop);
        }
        return shop;
    }
    private readonly System.Collections.Generic.Dictionary<int, MapEventVisit> eventVisits = new System.Collections.Generic.Dictionary<int, MapEventVisit>();

    //맵 지점별 이벤트 상태 생성과 기존 방문 재사용
    public MapEventVisit GetEventVisit(int nodeIndex, MapEventDefinition definition, CoinModel coins, OwnedDeckModel deck)
    {
        if (eventVisits.TryGetValue(nodeIndex, out var visit)) return visit;
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        visit = definition.CreateVisit(this, coins, deck, () => UnityEngine.Random.value);
        eventVisits.Add(nodeIndex, visit);
        return visit;
    }
    public int PlayerId { get; private set; }
    public CombatantState Health { get; private set; }
    public MapProgressModel Map { get; private set; }
    public int PendingEncounterId { get; private set; }
    public bool IsFinalBossBattle { get; private set; }
    public bool IsCleared { get; private set; }
    public bool HasPendingBattle => PendingEncounterId > 0;
    public float MapScrollX { get; private set; }
    public float MapScrollY { get; private set; }

    //전투 진입 전 맵 스크롤 위치 저장
    public void SaveMapScroll(float horizontal, float vertical)
    {
        MapScrollX = horizontal;
        MapScrollY = vertical;
    }

    //첫 플레이어 체력 준비와 캐릭터 변경 차단
    public void PreparePlayer(CharacterData character)
    {
        if (character == null) throw new ArgumentNullException(nameof(character));
        if (Health != null && PlayerId != character.Id)
            throw new InvalidOperationException("진행 중인 캐릭터 변경은 새 게임에서만 가능합니다");
        if (Health != null) return;
        PlayerId = character.Id;
        Health = new CombatantState(character.MaxHealth);
    }

    //맵 최초 생성과 씬 복귀 시 기존 진행 유지
    public void PrepareMap(Func<MapGraphData> create)
    {
        if (Map == null) Map = new MapProgressModel(create());
    }

    //전투 대상과 최종 보스 여부 저장
    public void SetBattle(int encounterId, bool isFinalBoss = false)
    {
        PendingEncounterId = encounterId;
        IsFinalBossBattle = isFinalBoss;
    }

    //최종 보스 승리의 진행 완료와 전투 정보 정리
    public void CompleteFinalBoss()
    {
        if (IsCleared || !HasPendingBattle || !IsFinalBossBattle ||
            Health == null || !Health.IsAlive || (Map != null && Map.IsFailed)) return;
        Map?.CompleteCurrent();
        IsCleared = true;
        ClearBattle();
    }

    //전투 체력만 저장하고 방어도와 일시 효과 제외
    public void CaptureHealth(CombatantState source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        Health = new CombatantState(source.MaxHealth, source.CurrentHealth);
    }

    //전투 진입 정보 제거
    public void ClearBattle()
    {
        PendingEncounterId = 0;
        IsFinalBossBattle = false;
    }

    //새 게임의 체력과 맵 및 전투 정보 초기화
    public void Reset()
    {
        eventVisits.Clear();
        shopVisits.Clear();
        PlayerId = 0;
        Health = null;
        Map = null;
        PendingEncounterId = 0;
        IsFinalBossBattle = false;
        IsCleared = false;
        MapScrollX = 0f;
        MapScrollY = 0f;
    }
}

//씬 객체가 세션에 요청하는 전투 이동 기능
public interface IRunSceneLoader
{
    bool IsLoading { get; }

    //전투 대상의 비동기 이동 요청
    bool EnterBattle(int encounterId, bool isFinalBoss = false);

    //승리 보상 완료 후 맵 씬 복귀 요청
    bool ReturnToMap();
}
