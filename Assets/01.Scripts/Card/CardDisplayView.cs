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
        if (!CacheOutline())
        {
            GameLogger.LogError("[카드 표시] 카드 배경의 Outline을 연결해 주세요", this);
            return;
        }
        outline.enabled = true;
        ApplyRarityColor(data.Rarity);
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
