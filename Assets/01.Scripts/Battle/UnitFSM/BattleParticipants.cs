using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum BattleResult { Ongoing, Victory, Defeat, Inactive }

//편성에 포함된 전투 참가자 생성과 개별 UI 연결
public class BattleParticipants : MonoBehaviour
{
    private IGameDataProvider dataProvider;
    private IBattleDeckManager deckManager;
    private int displayedStrength = -1;

    public event Action StrengthChanged;
    public event Action DamagePreviewChanged;
    public event Action<BattleResult> ResultChanged;
    public BattleResult Result { get; private set; } = BattleResult.Ongoing;
    public bool IsBattleActive => IsReady && Result == BattleResult.Ongoing &&
        Player.Stats.IsAlive && enemies.Any(enemy => enemy.Stats.IsAlive);

    //외부에서 전달한 데이터 조회 기능과 전투 덱 관리자 저장
    public void Inject(IGameDataProvider provider, IBattleDeckManager manager)
    {
        if (provider == null)
            throw new System.ArgumentNullException(nameof(provider));
        if (manager == null)
            throw new System.ArgumentNullException(nameof(manager));

        if (deckManager != null)
            deckManager.Changed -= RefreshDeckCounts;

        dataProvider = provider;
        deckManager = manager;
        deckManager.Changed += RefreshDeckCounts;
        RefreshDeckCounts();
    }

    [SerializeField] private BattleCombatantView playerView;
    [SerializeField] private BattleCombatantView enemyView;
    [SerializeField] private EnemyViewFactory enemyFactory;
    [SerializeField] private TMP_Text playerEnergyStatus;
    [SerializeField] private TMP_Text deckCountText;

    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private int playerId = 50001;
    [SerializeField] private int enemyId = 60001;
    public PlayerBattleState Player { get; private set; }
    private readonly List<EnemyBattleState> enemies = new();
    private readonly List<BattleCombatantView> enemyViews = new();
    public IReadOnlyList<EnemyBattleState> Enemies => enemies;
    public IReadOnlyList<BattleCombatantView> EnemyViews => enemyViews;
    //기존 디버그와 보상 코드의 전투 식별용 첫 적
    public EnemyBattleState Enemy => enemies.FirstOrDefault();
    public int EncounterId { get; private set; }
    public EnemyBattleState FirstAliveEnemy => enemies.FirstOrDefault(enemy => enemy.Stats.IsAlive);

    //살아 있는 적 중 무작위 대상 선택
    public EnemyBattleState GetRandomAliveEnemy()
    {
        var alive = enemies.Where(enemy => enemy.Stats.IsAlive).ToArray();
        return alive.Length == 0 ? null : alive[UnityEngine.Random.Range(0, alive.Length)];
    }

    //드래그한 UI에 연결된 살아 있는 적 조회
    public bool TryGetEnemy(BattleCombatantView view, out EnemyBattleState enemy)
    {
        int index = view == null ? -1 : enemyViews.IndexOf(view);
        enemy = index < 0 ? null : enemies[index];
        return enemy != null && enemy.Stats.IsAlive;
    }

    //현재 편성 그대로 테스트 전투 초기화
    public bool RestartBattle() => EncounterId > 0
        ? InitializeEncounter(Player.Data.Id, EncounterId)
        : Initialize(Player.Data.Id, Enemy.Data.Id);
    public BattleCombatantView PlayerView => playerView;
    public BattleCombatantView EnemyView => enemyView;
    public bool IsReady => Player != null && Enemy != null;

    //자동 시작이 켜져 있으면 설정된 ID로 참가자를 생성
    private void Start()
    {
        if (initializeOnStart && !IsReady) Initialize(playerId, enemyId);
    }

    //맵 진입 시 자동 전투 시작 해제
    public void UseMapFlow()
    {
        initializeOnStart = false;
        LeaveBattle();
    }

    //전투 입력과 보상 수령 종료
    public void LeaveBattle()
    {
        Result = BattleResult.Inactive;
        ResultChanged?.Invoke(Result);
    }

    //첫 전투 전 캠프에서 사용할 플레이어 상태 준비
    public bool PrepareMapPlayer(int characterId)
    {
        if (Player != null)
            return Player.Stats.IsAlive;
        if (dataProvider == null || !dataProvider.TryGetCharacter(characterId, out var character) ||
            playerView == null || playerEnergyStatus == null)
            return false;
        Player = new PlayerBattleState(character);
        Player.Changed += RefreshPlayer;
        Player.Stats.Changed += RefreshPlayer;
        Player.Stats.Changed += CheckBattleResult;
        playerView.Bind(character.DisplayName, character.Portrait, Player.Stats);
        RefreshPlayer();
        return true;
    }

#if UNITY_EDITOR
    //에디터 전투 중 넘패드 테스트 입력 처리
    private void Update()
    {
        if (!IsBattleActive || Keyboard.current == null || GUIUtility.keyboardControl != 0) return;

        if (Keyboard.current.numpad1Key.wasPressedThisFrame) DamageEnemy();
        if (Keyboard.current.numpad2Key.wasPressedThisFrame) DamagePlayer();
        if (Keyboard.current.numpad3Key.wasPressedThisFrame) BlockPlayer();
        if (Keyboard.current.numpad4Key.wasPressedThisFrame) HealPlayer();
        if (Keyboard.current.numpad5Key.wasPressedThisFrame) SpendEnergy();
        if (Keyboard.current.numpad6Key.wasPressedThisFrame) PlanEnemy();
        if (Keyboard.current.numpad7Key.wasPressedThisFrame) ResetParticipants();
        if (Keyboard.current.numpad8Key.wasPressedThisFrame) WeakenPlayer();
        if (Keyboard.current.numpad9Key.wasPressedThisFrame) VulnerablePlayer();
    }
#endif

    //전투 편성 조회 후 빈 자리를 유지한 참가자 생성
    public bool InitializeEncounter(int characterId, int encounterId, int? currentHealth = null)
    {
        if (dataProvider == null || !dataProvider.TryGetEncounter(encounterId, out var encounter))
        {
            GameLogger.LogError($"[전투] 편성 {encounterId}를 찾지 못했습니다", this);
            return false;
        }
        return InitializeEnemies(characterId, encounter.EnemyIds, false, currentHealth, encounterId);
    }

    //기존 단일 적 테스트 진입점
    public bool Initialize(int characterId, int targetEnemyId, bool preserveHealth = false, int? currentHealth = null) =>
        InitializeEnemies(characterId, new[] { targetEnemyId }, preserveHealth, currentHealth, 0);

    //배치와 데이터 검증 후 개별 상태 및 외형 생성
    private bool InitializeEnemies(int characterId, IReadOnlyList<int> ids, bool preserveHealth,
        int? currentHealth, int encounterId)
    {
        if (dataProvider == null || !dataProvider.IsReady || playerView == null ||
            playerEnergyStatus == null || deckManager == null || deckCountText == null ||
            ids == null || ids.Count == 0 || ids.Count > 3 || !ids.Any(id => id > 0) ||
            !dataProvider.TryGetCharacter(characterId, out var character)) return false;

        var definitions = new List<(EnemyData data, int slot)>();
        for (int slot = 0; slot < ids.Count; slot++)
        {
            if (ids[slot] == 0) continue;
            if (!dataProvider.TryGetEnemy(ids[slot], out var definition) ||
                (enemyFactory != null ? !enemyFactory.CanCreateAt(slot) : slot != 0 || enemyView == null))
            {
                GameLogger.LogError($"[전투] 적 {ids[slot]} 데이터 또는 EnemyPoint{slot + 1} 연결 확인", this);
                return false;
            }
            definitions.Add((definition, slot));
        }
        var nextPlayer = new PlayerBattleState(character);
        int health = currentHealth ?? (preserveHealth && Player != null ? Player.Stats.CurrentHealth : character.MaxHealth);
        if (health <= 0) return false;
        nextPlayer.Stats.TakeDamage(Math.Max(0, nextPlayer.Stats.MaxHealth - health));
        var random = new System.Random();
        var nextEnemies = definitions.Select(entry => new EnemyBattleState(entry.data, random)).ToList();
        UnsubscribeParticipantEvents();
        enemyFactory?.Clear();
        enemies.Clear();
        enemyViews.Clear();
        Player = nextPlayer;
        EncounterId = encounterId;
        Result = BattleResult.Inactive;
        for (int index = 0; index < definitions.Count; index++)
        {
            var definition = definitions[index];
            var state = nextEnemies[index];
            var view = enemyFactory != null
                ? enemyFactory.Create(definition.data, state.Stats, definition.slot) : enemyView;
            if (view == null)
            {
                enemyFactory?.Clear();
                enemies.Clear();
                enemyViews.Clear();
                return false;
            }
            if (enemyFactory == null)
                view.Bind(definition.data.DisplayName, null, state.Stats, definition.data.BattlePrefab, true);
            enemies.Add(state);
            enemyViews.Add(view);
        }
        enemyView = enemyViews[0];
        Player.Changed += RefreshPlayer;
        Player.Stats.Changed += RefreshPlayer;
        Player.Stats.Changed += CheckBattleResult;
        Player.Stats.Changed += RefreshDamagePreviews;
        foreach (var enemy in enemies)
        {
            enemy.Stats.Changed += CheckBattleResult;
            enemy.Stats.Changed += RefreshDamagePreviews;
            enemy.IntentChanged += RefreshEnemy;
        }
        playerView.Bind(character.DisplayName, character.Portrait, Player.Stats, character.BattlePrefab);
        Result = BattleResult.Ongoing;
        RefreshPlayer();
        RefreshEnemy();
        ResultChanged?.Invoke(Result);
        return true;
    }

    //참가자 사망 여부에 따른 승패 확정과 결과 알림
    private void CheckBattleResult()
    {
        if (!IsReady || Result != BattleResult.Ongoing)
            return;

        if (!Player.Stats.IsAlive)
            Result = BattleResult.Defeat;
        else if (enemies.All(enemy => !enemy.Stats.IsAlive))
            Result = BattleResult.Victory;
        else
            return;

        ResultChanged?.Invoke(Result);
    }

    //플레이어 상태 표시와 힘 변경 알림
    private void RefreshPlayer()
    {
        playerEnergyStatus.text = $"에너지  {Player.Energy} / {Player.Data.StartingEnergy}";
        playerView.SetDetail($"{Player.Data.ResourceName} 사용 캐릭터");

        int strength = Player.Stats.Strength;
        if (displayedStrength != strength)
        {
            displayedStrength = strength;
            StrengthChanged?.Invoke();
        }
    }

    //뽑기 더미와 버린 더미 카드 수 갱신
    private void RefreshDeckCounts()
    {
        if (deckCountText != null)
            deckCountText.text = $"덱 {deckManager.DrawCount} / {deckManager.DiscardCount}";
    }

    //각 적의 다음 행동과 보정된 공격 수치 표시
    private void RefreshEnemy()
    {
        for (int index = 0; index < enemies.Count; index++)
        {
            var enemy = enemies[index];
            var view = enemyViews[index];
            if (!enemy.Stats.IsAlive || !enemy.PlannedAction.HasValue)
            {
                view.SetDetail("다음 행동 없음");
                continue;
            }
            var action = enemy.PlannedAction.Value;
            var intent = action.Intent switch
            {
                IntentType.Attack => "공격", IntentType.Defend => "방어",
                IntentType.Buff => "강화", IntentType.Debuff => "약화", _ => "특수"
            };
            view.SetDetail($"다음 행동: {intent}\n" + string.Join(" / ",
                action.Effects.Select(effect => $"{effect.Type} {enemy.GetPreviewAmount(effect, Player.Stats)}")));
        }
    }

    //양쪽 상태 변경 시 손패 설명과 적 공격 예고 갱신
    private void RefreshDamagePreviews()
    {
        if (!IsReady) return;
        RefreshEnemy();
        DamagePreviewChanged?.Invoke();
    }

    //컴포넌트 제거 시 모든 상태 변경 구독 해제
    private void OnDestroy()
    {
        UnsubscribeParticipantEvents();

        if (deckManager != null)
            deckManager.Changed -= RefreshDeckCounts;
    }

    //기존 참가자의 UI 갱신 이벤트 구독 해제
    private void UnsubscribeParticipantEvents()
    {
        if (Player != null)
        {
            Player.Changed -= RefreshPlayer;
            Player.Stats.Changed -= RefreshPlayer;
            Player.Stats.Changed -= CheckBattleResult;
            Player.Stats.Changed -= RefreshDamagePreviews;
        }
        foreach (var enemy in enemies)
        {
            enemy.IntentChanged -= RefreshEnemy;
            enemy.Stats.Changed -= CheckBattleResult;
            enemy.Stats.Changed -= RefreshDamagePreviews;
        }
    }

#if UNITY_EDITOR
    //테스트용으로 적에게 피해 10을 부여
    private void DamageEnemy()
    {
        if (!Application.isPlaying || !IsReady) return;
        FirstAliveEnemy?.Stats.TakeDamage(10);
        GameLogger.Log($"[전투 테스트] 적 피해 10 / HP {Enemy.Stats.CurrentHealth}", this);
    }

    //테스트용으로 플레이어에게 피해 10을 부여
    private void DamagePlayer()
    {
        if (!Application.isPlaying || !IsReady) return;
        Player.Stats.TakeDamage(10);
        GameLogger.Log($"[전투 테스트] 플레이어 피해 10 / HP {Player.Stats.CurrentHealth}", this);
    }

    //테스트용으로 플레이어에게 방어도 5를 추가
    private void BlockPlayer()
    {
        if (!Application.isPlaying || !IsReady) return;
        Player.Stats.GainBlock(5);
        GameLogger.Log($"[전투 테스트] 플레이어 방어 +5 / 방어 {Player.Stats.Block}", this);
    }

    //테스트용으로 플레이어의 체력을 10 회복
    private void HealPlayer()
    {
        if (!Application.isPlaying || !IsReady) return;
        Player.Stats.Heal(10);
        GameLogger.Log($"[전투 테스트] 플레이어 회복 10 / HP {Player.Stats.CurrentHealth}", this);
    }

    //테스트용으로 에너지 1 소비를 시도
    private void SpendEnergy()
    {
        if (!Application.isPlaying || !IsReady) return;
        bool spent = Player.TrySpendEnergy(1);
        GameLogger.Log($"[전투 테스트] 에너지 사용 {(spent ? "성공" : "실패")} / 에너지 {Player.Energy}", this);
    }

    //테스트용으로 적의 다음 행동을 다시 선택
    private void PlanEnemy()
    {
        if (!Application.isPlaying || !IsReady) return;
        foreach (var enemy in enemies) enemy.PlanNextAction();
        GameLogger.Log("[전투 테스트] 적 다음 행동 재선택", this);
    }

    //현재 전투의 참가자를 기본 체력으로 다시 생성
    private void ResetParticipants()
    {
        if (!Application.isPlaying || !IsBattleActive) return;
        RestartBattle();
        GameLogger.Log("[전투 테스트] 참가자 초기화", this);
    }

    //테스트용으로 플레이어에게 약화 1을 부여
    private void WeakenPlayer()
    {
        if (!Application.isPlaying || !IsReady) return;
        Player.Stats.ApplyWeak(1);
        GameLogger.Log($"[전투 테스트] 플레이어 약화 +1 / 약화 {Player.Stats.Weak}", this);
    }

    //테스트용으로 플레이어에게 취약 1을 부여
    private void VulnerablePlayer()
    {
        if (!Application.isPlaying || !IsReady) return;
        Player.Stats.ApplyVulnerable(1);
        GameLogger.Log($"[전투 테스트] 플레이어 취약 +1 / 취약 {Player.Stats.Vulnerable}", this);
    }
#endif
}
