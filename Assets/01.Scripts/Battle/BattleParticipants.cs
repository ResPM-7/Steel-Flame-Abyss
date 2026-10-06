using System;
using System.Linq;
using SteelFlameAbyss.Data;
using TMPro;
using UnityEngine;

namespace SteelFlameAbyss.Battle
{
    /// <summary>첫 1:1 전투의 참가자를 SO에서 만들고 UI에 연결합니다.</summary>
    public sealed class BattleParticipants : MonoBehaviour
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

        private void Start()
        {
            if (initializeOnStart && !IsReady) Initialize(playerId, enemyId);
        }

        /// <summary>이후 BattleController에서 캐릭터와 조우 ID를 전달해 호출합니다.</summary>
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

        private void RefreshPlayer()
        {
            playerStatusLabel.text = $"에너지  {Player.Energy} / {Player.Data.StartingEnergy}";
            playerView.SetDetail($"{Player.Data.ResourceName} 사용 캐릭터");
        }

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

        private void OnDestroy() => Unsubscribe();
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
        [ContextMenu("테스트/적에게 피해 10")]
        private void DamageEnemy() { if (Application.isPlaying && IsReady) Enemy.Stats.TakeDamage(10); }
        [ContextMenu("테스트/플레이어에게 피해 10")]
        private void DamagePlayer() { if (Application.isPlaying && IsReady) Player.Stats.TakeDamage(10); }
        [ContextMenu("테스트/플레이어 방어 5")]
        private void BlockPlayer() { if (Application.isPlaying && IsReady) Player.Stats.GainBlock(5); }
        [ContextMenu("테스트/플레이어 회복 10")]
        private void HealPlayer() { if (Application.isPlaying && IsReady) Player.Stats.Heal(10); }
        [ContextMenu("테스트/에너지 1 사용")]
        private void SpendEnergy() { if (Application.isPlaying && IsReady) Player.TrySpendEnergy(1); }
        [ContextMenu("테스트/적 다음 행동 선택")]
        private void PlanEnemy() { if (Application.isPlaying && IsReady) Enemy.PlanNextAction(); }
        [ContextMenu("테스트/참가자 초기화")]
        private void ResetParticipants() { if (Application.isPlaying) Initialize(playerId, enemyId); }
    }
}
