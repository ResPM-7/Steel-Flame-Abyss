using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

//유물 아이콘 표시와 포인터 진입·이탈 전달
public class RelicIconView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UnityEngine.UI.Image icon;
    [SerializeField] private TMP_Text fallbackLabel;
    private RelicData data;
    private RelicPanelView owner;

    public void Bind(RelicData value, RelicPanelView panel)
    {
        data = value;
        owner = panel;
        icon.sprite = null;
        icon.color = Color.white;
        fallbackLabel.text = value.DisplayName;
        fallbackLabel.gameObject.SetActive(true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (data != null) owner.ShowTooltip(this, data);
    }

    //풀 반환 전에 이전 유물과 패널 참조 제거
    public void Unbind()
    {
        owner?.HideTooltip(this);
        owner = null;
        data = null;
        fallbackLabel.text = string.Empty;
    }

    public void OnPointerExit(PointerEventData eventData) => owner?.HideTooltip(this);
    private void OnDisable() => owner?.HideTooltip(this);
}
