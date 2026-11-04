using UnityEngine;

//플레이어와 적이 함께 사용하는 상태 아이콘 보관
[CreateAssetMenu(fileName = "BattleStatusIcons", menuName = "Game Data/Battle Status Icons")]
public class BattleStatusIcons : ScriptableObject
{
    [Tooltip("화상")]
    [SerializeField] private Sprite burn;
    [Tooltip("화상 반격")]
    [SerializeField] private Sprite burnRetaliation;
    [Tooltip("약화")]
    [SerializeField] private Sprite weak;
    [Tooltip("취약")]
    [SerializeField] private Sprite vulnerable;
    [Tooltip("정신분열")]
    [SerializeField] private Sprite mindFracture;
    [Tooltip("힘")]
    [SerializeField] private Sprite strength;

    public Sprite Burn => burn;
    public Sprite BurnRetaliation => burnRetaliation;
    public Sprite Weak => weak;
    public Sprite Vulnerable => vulnerable;
    public Sprite MindFracture => mindFracture;
    public Sprite Strength => strength;
}
