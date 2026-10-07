using System;
using System.Linq;
using SteelFlameAbyss.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SteelFlameAbyss.Battle
{
    //첫 1:1 전투의 참가자를 SO에서 만들고 UI에 연결합니다.
    public class BattleParticipants : MonoBehaviour
    {
        [SerializeField] private GameDataProvider dataProvider;
        [SerializeField] private BattleCombatantView playerView;
        [SerializeField] private BattleCombatantView enemyView;
        [SerializeField] private TMP_Text playerStatusLabel;
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private int playerId = 50001;
        [SerializeField] private int enemyId = 60001;
        public PlayerBattleState Player { get; private set; }
        public EnemyBattleState Enemy { get; private set; }
        public bool IsReady => Player != null && Enemy != null;

        //자동 시작이 켜져 있으면 설정된 ID로 참가자를 생성합니다.
        private void Start()
        {
            if (initializeOnStart && !IsReady) Initialize(playerId, enemyId);
        }

        //넘패드 1~9 입력을 읽어 전투 테스트 기능을 실행합니다.
        private void Update()
        {
            if (!IsReady || Keyboard.current == null) return;

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

        //이후 BattleController에서 캐릭터와 조우 ID를 전달해 호출합니다.
        public bool Initialize(int characterId, int targetEnemyId)
        {
            if (dataProvider == null || !dataProvider.IsReady || playerView == null ||
                enemyView == null || playerStatusLabel == null)
            {
                Debug.LogError("[전투] BattleParticipants의 데이터 및 UI 연결을 확인해 주세요.", this);
                return false;
            }
            if (!dataProvider.TryGetCharacter(characterId, out var character) ||
                !dataProvider.TryGetEnemy(targetEnemyId, out var enemy))
            {
                Debug.LogError($"[전투] 캐릭터 {characterId} 또는 적 {targetEnemyId}를 찾지 못했습니다.", this);
                return false;
            }

            var nextPlayer = new PlayerBattleState(character);
            var nextEnemy = new EnemyBattleState(enemy, new System.Random());
            Unsubscribe();
            Player = nextPlayer;
            Enemy = nextEnemy;
            Player.Changed += RefreshPlayer;
            Player.Stats.Changed += RefreshPlayer;
            Enemy.IntentChanged += RefreshEnemy;
            playerView.Bind(character.DisplayName, character.Portrait, Player.Stats);
            enemyView.Bind(enemy.DisplayName, enemy.Artwork, Enemy.Stats);
            RefreshPlayer();
            RefreshEnemy();
            return true;
        }

        //플레이어의 에너지와 고유 자원 설명을 갱신합니다.
        private void RefreshPlayer()
        {
            playerStatusLabel.text = $"에너지  {Player.Energy} / {Player.Data.StartingEnergy}";
            playerView.SetDetail($"{Player.Data.ResourceName} 사용 캐릭터");
        }

        //적의 다음 행동과 효과 수치를 표시합니다.
        private void RefreshEnemy()
        {
            if (!Enemy.PlannedAction.HasValue) { enemyView.SetDetail("다음 행동 없음"); return; }
            var action = Enemy.PlannedAction.Value;
            var intent = action.Intent switch
            {
                IntentType.Attack => "공격", IntentType.Defend => "방어",
                IntentType.Buff => "강화", IntentType.Debuff => "약화", _ => "특수"
            };
            enemyView.SetDetail($"다음 행동: {intent}\n" +
                string.Join(" / ", action.Effects.Select(e => $"{e.Type} {e.Amount}")));
        }

        //컴포넌트가 제거되면 상태 변경 구독을 해제합니다.
        private void OnDestroy() => Unsubscribe();

        //기존 참가자의 UI 갱신 이벤트 구독을 해제합니다.
        private void Unsubscribe()
        {
            if (Player != null)
            {
                Player.Changed -= RefreshPlayer;
                Player.Stats.Changed -= RefreshPlayer;
            }
            if (Enemy != null) Enemy.IntentChanged -= RefreshEnemy;
        }

        // 인스펙터 컴포넌트 메뉴에서 Play Mode에만 수행하는 수동 검증입니다.
        //테스트용으로 적에게 피해 10을 줍니다.
        private void DamageEnemy()
        {
            if (!Application.isPlaying || !IsReady) return;
            Enemy.Stats.TakeDamage(10);
            Debug.Log($"[전투 테스트] 적 피해 10 / HP {Enemy.Stats.CurrentHealth}", this);
        }

        //테스트용으로 플레이어에게 피해 10을 줍니다.
        private void DamagePlayer()
        {
            if (!Application.isPlaying || !IsReady) return;
            Player.Stats.TakeDamage(10);
            Debug.Log($"[전투 테스트] 플레이어 피해 10 / HP {Player.Stats.CurrentHealth}", this);
        }

        //테스트용으로 플레이어에게 방어도 5를 추가합니다.
        private void BlockPlayer()
        {
            if (!Application.isPlaying || !IsReady) return;
            Player.Stats.GainBlock(5);
            Debug.Log($"[전투 테스트] 플레이어 방어 +5 / 방어 {Player.Stats.Block}", this);
        }

        //테스트용으로 플레이어의 체력을 10 회복합니다.
        private void HealPlayer()
        {
            if (!Application.isPlaying || !IsReady) return;
            Player.Stats.Heal(10);
            Debug.Log($"[전투 테스트] 플레이어 회복 10 / HP {Player.Stats.CurrentHealth}", this);
        }

        //테스트용으로 에너지 1 소비를 시도합니다.
        private void SpendEnergy()
        {
            if (!Application.isPlaying || !IsReady) return;
            bool spent = Player.TrySpendEnergy(1);
            Debug.Log($"[전투 테스트] 에너지 사용 {(spent ? "성공" : "실패")} / 에너지 {Player.Energy}", this);
        }

        //테스트용으로 적의 다음 행동을 다시 선택합니다.
        private void PlanEnemy()
        {
            if (!Application.isPlaying || !IsReady) return;
            Enemy.PlanNextAction();
            Debug.Log("[전투 테스트] 적 다음 행동 재선택", this);
        }

        //테스트용으로 참가자를 처음 상태로 다시 생성합니다.
        private void ResetParticipants()
        {
            if (!Application.isPlaying) return;
            Initialize(playerId, enemyId);
            Debug.Log("[전투 테스트] 참가자 초기화", this);
        }

        //테스트용으로 플레이어에게 약화 1을 부여합니다.
        private void WeakenPlayer()
        {
            if (!Application.isPlaying || !IsReady) return;
            Player.Stats.ApplyWeak(1);
            Debug.Log($"[전투 테스트] 플레이어 약화 +1 / 약화 {Player.Stats.Weak}", this);
        }

        //테스트용으로 플레이어에게 취약 1을 부여합니다.
        private void VulnerablePlayer()
        {
            if (!Application.isPlaying || !IsReady) return;
            Player.Stats.ApplyVulnerable(1);
            Debug.Log($"[전투 테스트] 플레이어 취약 +1 / 취약 {Player.Stats.Vulnerable}", this);
        }
    }
}
