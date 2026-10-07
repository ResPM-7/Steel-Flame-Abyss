using System;
using System.Collections.Generic;
using UnityEngine;

//시트에는 아래 enum 이름을 그대로 입력 대소문자는 구분하지 않음
public enum CharacterClass { Common, Warrior, FireMage, Warlock }
public enum CardType { Attack, Skill, Power, Status }
public enum DataRarity { Starter, Common, Uncommon, Rare, Special }
public enum EnemyTier { Normal, Elite, Boss }
public enum TargetType { Self, SingleEnemy, AllEnemies, RandomEnemy, None }
public enum IntentType { Attack, Defend, Buff, Debuff, Special }
public enum RelicTrigger
{
    Passive = 0,
    BattleStart = 1,
    TurnStart = 2,
    OnCardPlayed = 3,
    CardPlayed = OnCardPlayed,
    AttackCardPlayed = 4,
    EnemyDefeated = 5,
    BattleReward = 6,
    TurnEnd = 7,
    BattleEnd = 8,
    OnDamageTaken = 9
}

public enum EffectType
{
    Damage = 0,
    Block = 1,
    Strength = 2,       //힘증가

    //전사 능력
    Rage = 11,           //분노증가
    RageFinisher = 12,   //분노사용 결정타

    //불마법사 능력
    Burn = 21,           //화상
    Ignite = 22,         //화상즉시 대미지
    Overheat = 23,       //과열부여

    //흑마법사 능력
    MindFracture = 31,          //정신분열부여
    AmplifyMindFracture = 32,   //현재정신분열 배수
    NextMindFractureBonus = 33, //다음카드 정신분열 추가

    Draw = 101,
    GainEnergy = 102,
    Exhaust = 103,            //소멸
    Weak = 104,               //약화(공격감소)
    Vulnerable = 105,         //취약(들어가는 데미지 증가)
    AttackDamageBonus = 106,  //공격카드 피해 증가

    GoldMultiplier = 199      //골드배율
}

[Serializable]
public struct EffectSpec
{
    //하나의 카드/유물 효과를 데이터만으로 조합하기 위한 공통 명세
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
    //적 행동 하나는 의도 선택 가중치 실제 효과 목록으로 구성
    [SerializeField] private string actionName;
    [SerializeField] private IntentType intent;
    [Min(0f)] [SerializeField] private float weight;
    [SerializeField] private List<EffectSpec> effects;

    public string ActionName => actionName;
    public IntentType Intent => intent;
    public float Weight => weight;
    public IReadOnlyList<EffectSpec> Effects => effects;

    public EnemyActionSpec(IntentType intent, float weight, List<EffectSpec> effects)
        : this(intent.ToString(), intent, weight, effects)
    {
    }

    public EnemyActionSpec(string actionName, IntentType intent, float weight, List<EffectSpec> effects)
    {
        this.actionName = string.IsNullOrWhiteSpace(actionName) ? intent.ToString() : actionName;
        this.intent = intent;
        this.weight = weight;
        this.effects = effects ?? new List<EffectSpec>();
    }
}
