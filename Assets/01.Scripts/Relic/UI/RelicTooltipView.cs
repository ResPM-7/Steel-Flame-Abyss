using TMPro;
using UnityEngine;

//아이콘 아래에 유물 설명을 표시하고 화면 경계 안으로 보정
public class RelicTooltipView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text descriptionText;
    private readonly Vector3[] corners = new Vector3[4];

    public void Show(RelicData data, RectTransform target)
    {
        titleText.text = data.DisplayName;
        rarityText.text = data.Rarity.ToString();
        descriptionText.text = data.Description;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        RectTransform rect = (RectTransform)transform;
        RectTransform parent = (RectTransform)transform.parent;
        Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        target.GetWorldCorners(corners);
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, camera, out Vector2 position);
        position.y -= 8f;
        Rect bounds = parent.rect;
        position.x = Mathf.Clamp(position.x, bounds.xMin, Mathf.Max(bounds.xMin, bounds.xMax - rect.rect.width));
        position.y = Mathf.Clamp(position.y, Mathf.Min(bounds.yMax, bounds.yMin + rect.rect.height), bounds.yMax);
        rect.localPosition = new Vector3(position.x, position.y, 0f);
    }

    public void Hide() => gameObject.SetActive(false);
}
