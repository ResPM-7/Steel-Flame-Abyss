using TMPro;
using UnityEngine;

//모든 카드의 공통 텍스트와 희귀도 테두리 표시
public class CardDisplayView : MonoBehaviour
{
    [SerializeField] protected TMP_Text nameText;
    [SerializeField] protected TMP_Text costText;
    [SerializeField] protected TMP_Text descriptionText;
    [SerializeField] private UnityEngine.UI.Outline outline;
    [SerializeField] private CardRarityColors rarityColors;
    [Header("카드 공통 디자인")]
    [SerializeField] private UnityEngine.UI.Image classFrame;
    [SerializeField] private UnityEngine.UI.Image costBadge;
    [SerializeField] private UnityEngine.UI.Image artworkWell;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private Color steelColor = new Color(0.38f, 0.63f, 0.82f);
    [SerializeField] private Color flameColor = new Color(0.94f, 0.43f, 0.18f);
    [SerializeField] private Color abyssColor = new Color(0.66f, 0.42f, 0.85f);
    [SerializeField] private Color publicColor = new Color(0.72f, 0.66f, 0.44f);

    public virtual bool IsConfigured => nameText != null && costText != null && descriptionText != null && CacheOutline();

    //지정되지 않은 테두리를 단일 자식 테두리에서 저장
    private bool CacheOutline()
    {
        if (outline != null)
            return true;
        var outlines = GetComponentsInChildren<UnityEngine.UI.Outline>(true);
        if (outlines.Length == 1)
            outline = outlines[0];
        return outline != null;
    }

    //카드 정보와 희귀도 색상을 공통 화면에 반영
    protected void SetDisplay(CardData data, int cost, string description)
    {
        nameText.text = data.DisplayName;
        costText.text = cost.ToString();
        descriptionText.text = description;
        ApplyDesign(data);
        if (!CacheOutline())
        {
            GameLogger.LogError("[카드 표시] 카드 배경의 Outline을 연결해 주세요", this);
            return;
        }
        outline.enabled = true;
        ApplyRarityColor(data.Rarity);
    }

    //직업별 프레임 색상과 카드 종류 표시, 일러스트 영역 비워두기
    private void ApplyDesign(CardData data)
    {
        Color accent = data.Owner switch
        {
            CharacterClass.Warrior => steelColor,
            CharacterClass.FireMage => flameColor,
            CharacterClass.Warlock => abyssColor,
            _ => publicColor
        };
        Color emphasis = data.Rarity == DataRarity.Special ? new Color(0.88f, 0.69f, 0.3f) : accent;
        if (classFrame != null) classFrame.color = emphasis;
        if (costBadge != null) costBadge.color = Color.Lerp(new Color(0.06f, 0.07f, 0.09f), emphasis, 0.65f);
        if (artworkWell != null) artworkWell.color = Color.Lerp(new Color(0.055f, 0.06f, 0.08f), accent, 0.1f);
        if (typeText != null)
        {
            typeText.color = accent;
            typeText.text = data.CardType switch
            {
                CardType.Attack => "공격",
                CardType.Skill => "스킬",
                CardType.Power => "파워",
                CardType.Status => "상태",
                _ => data.CardType.ToString()
            };
        }
    }

    //공통 설정의 희귀도 색상을 카드 테두리에 적용
    private void ApplyRarityColor(DataRarity rarity)
    {
        if (rarityColors == null)
        {
            GameLogger.LogWarning("[카드 표시] 공통 CardRarityColors 에셋을 연결해 주세요", this);
            return;
        }
        outline.effectColor = rarityColors.GetColor(rarity);
    }
}
