using System;
using System.Collections.Generic;
using UnityEngine;

namespace SteelFlameAbyss.Data
{
    // 시트에는 아래 enum 이름을 그대로 입력합니다. 대소문자는 구분하지 않습니다.
    public enum CharacterClass { Common, Warrior, FireMage, Warlock }
    public enum CardType { Attack, Skill, Power, Status }
    public enum DataRarity { Starter, Common, Uncommon, Rare, Special }
    public enum EnemyTier { Normal, Elite, Boss }
    public enum TargetType { Self, SingleEnemy, AllEnemies, RandomEnemy }
    public enum IntentType { Attack, Defend, Buff, Debuff, Special }
    public enum RelicTrigger { Passive, BattleStart, TurnStart, CardPlayed, AttackCardPlayed, EnemyDefeated, BattleReward }

    public enum EffectType
    {
        Damage,
        Block,
        Strength,
        Rage,
        RageFinisher,
        Burn,
        Ignite,
        RetaliateBurn,
        Overheat,
        MindFracture,
        AmplifyMindFracture,
        NextMindFractureBonus,
        Draw,
        GainEnergy,
        Exhaust,
        CreateStatusCard,
        Weak,
        Vulnerable,
        AttackDamageBonus,
        GoldMultiplier
    }

    [Serializable]
    public struct EffectSpec
    {
        // 하나의 카드/유물 효과를 데이터만으로 조합하기 위한 공통 명세입니다.
        [SerializeField] private EffectType type;
        [SerializeField] private TargetType target;
        [SerializeField] private int amount;
        [SerializeField] private int secondaryAmount;
        [SerializeField] private int duration;

        public EffectType Type => type;
        public TargetType Target => target;
        public int Amount => amount;
        public int SecondaryAmount => secondaryAmount;
        public int Duration => duration;

        public EffectSpec(EffectType type, TargetType target, int amount, int secondaryAmount = 0, int duration = 0)
        {
            this.type = type;
            this.target = target;
            this.amount = amount;
            this.secondaryAmount = secondaryAmount;
            this.duration = duration;
        }
    }

    [Serializable]
    public struct EnemyActionSpec
    {
        // 적 행동 하나는 의도, 선택 가중치, 실제 효과 목록으로 구성됩니다.
        [SerializeField] private IntentType intent;
        [Min(0f)] [SerializeField] private float weight;
        [SerializeField] private List<EffectSpec> effects;

        public IntentType Intent => intent;
        public float Weight => weight;
        public IReadOnlyList<EffectSpec> Effects => effects;

        public EnemyActionSpec(IntentType intent, float weight, List<EffectSpec> effects)
        {
            this.intent = intent;
            this.weight = weight;
            this.effects = effects ?? new List<EffectSpec>();
        }
    }
}
