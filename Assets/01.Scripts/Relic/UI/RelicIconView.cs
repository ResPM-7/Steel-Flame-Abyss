using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

//유물 아이콘 표시와 포인터 진입·이탈 전달
public sealed class RelicIconView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UnityEngine.UI.Image icon;
    [SerializeField] private TMP_Text fallbackLabel;
    private RelicData data;
    private RelicPanelView owner;

    public void Bind(RelicData value, RelicPanelView panel)
    {
        data = value;
        owner = panel;
        //현재는 이미지 대신 흰 네모와 유물 이름만 표시
        icon.sprite = null;
        icon.color = Color.white;
        fallbackLabel.text = value.DisplayName;
        fallbackLabel.gameObject.SetActive(true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (data != null) owner.ShowTooltip(this, data);
    }

    public void OnPointerExit(PointerEventData eventData) => owner?.HideTooltip(this);
    private void OnDisable() => owner?.HideTooltip(this);
}
