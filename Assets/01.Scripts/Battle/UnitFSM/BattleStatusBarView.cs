using TMPro;
using UnityEngine;

//전투 상태 아이콘과 설명창 생성 및 재사용
public class BattleStatusBarView : MonoBehaviour
{
    private readonly BattleStatusItemView[] items = new BattleStatusItemView[5];
    private readonly Vector3[] corners = new Vector3[4];
    private RectTransform tooltip;
    private TMP_Text tooltipText;
    private BattleStatusItemView tooltipOwner;
    private TMP_FontAsset font;

    //기존 상태 텍스트 아래에 공용 상태 표시 줄 배치
    public static BattleStatusBarView Create(TMP_Text source)
    {
        RectTransform rect = NewRect("StatusEffects", source.transform.parent);
        RectTransform original = source.rectTransform;
        rect.anchorMin = original.anchorMin;
        rect.anchorMax = original.anchorMax;
        rect.pivot = original.pivot;
        rect.sizeDelta = new Vector2(412f, 44f);
        rect.anchoredPosition = original.anchoredPosition + Vector2.down * (original.rect.height * 0.5f + 30f);
        var layout = rect.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        var bar = rect.gameObject.AddComponent<BattleStatusBarView>();
        bar.font = source.font;
        bar.CreateTooltip(source.GetComponentInParent<Canvas>().rootCanvas);
        bar.items[0] = bar.CreateItem("화상", "턴 종료 시 수치만큼 방어도를 무시하고 체력 피해.\n발동 후 화상 수치 1 감소.", new Color(0.65f, 0.18f, 0.08f));
        bar.items[1] = bar.CreateItem("화상\n반격", "적의 공격을 받고 생존하면 공격자에게 수치만큼 화상 부여.\n방어도로 막아도 발동. 다음 자신의 턴 시작 시 해제.", new Color(0.48f, 0.24f, 0.08f));
        bar.items[2] = bar.CreateItem("약화", "공격 피해가 약화 수치만큼 감소.\n자신의 턴 종료 시 전체 해제.", new Color(0.25f, 0.35f, 0.25f));
        bar.items[3] = bar.CreateItem("취약", "받는 공격 피해 50% 증가.\n수치는 남은 턴. 자신의 턴 종료 시 1 감소.", new Color(0.38f, 0.22f, 0.48f));
        bar.items[4] = bar.CreateItem("정신\n분열", "수치가 적의 현재 체력 이상이면 처형.\n부여·증가 직후와 턴 종료 화상 처리 후 검사.", new Color(0.24f, 0.16f, 0.42f));
        return bar;
    }

    //살아 있는 유닛의 활성 상태만 표시
    public void Refresh(CombatantState state)
    {
        items[0].Refresh(state.IsAlive ? state.Burn : 0);
        items[1].Refresh(state.IsAlive ? state.BurnRetaliation : 0);
        items[2].Refresh(state.IsAlive ? state.Weak : 0);
        items[3].Refresh(state.IsAlive ? state.Vulnerable : 0);
        items[4].Refresh(state.IsAlive ? state.MindFracture : 0);
        //상태 다섯 개 동시 표시 시 기존 영역 안으로 폭 조정
        int count = 0;
        foreach (var item in items)
            if (item.gameObject.activeSelf) count++;
        float width = count > 0 ? Mathf.Min(100f, (412f - 4f * (count - 1)) / count) : 100f;
        foreach (var item in items)
            ((RectTransform)item.transform).sizeDelta = new Vector2(width, 44f);
    }

    //임시 이미지 안에 이름 배치 및 오른쪽 수치 배치
    private BattleStatusItemView CreateItem(string title, string description, Color color)
    {
        RectTransform root = NewRect(title.Replace("\n", ""), transform);
        root.sizeDelta = new Vector2(100f, 44f);
        var hit = root.gameObject.AddComponent<UnityEngine.UI.Image>();
        hit.color = new Color(0, 0, 0, 0);
        RectTransform icon = NewRect("Icon", root);
        icon.anchorMin = new Vector2(0, 0);
        icon.anchorMax = new Vector2(0.7f, 1);
        icon.offsetMin = icon.offsetMax = Vector2.zero;
        var background = icon.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.color = color;
        background.raycastTarget = false;
        TMP_Text label = NewText(icon, 18f);
        label.text = title;
        TMP_Text number = NewText(root, 22f);
        number.rectTransform.anchorMin = new Vector2(0.7f, 0);
        number.text = "0";
        var item = root.gameObject.AddComponent<BattleStatusItemView>();
        item.Initialize(this, number, title.Replace("\n", " "), description);
        root.gameObject.SetActive(false);
        return item;
    }

    //다른 UI 입력을 가리지 않는 설명창 생성
    private void CreateTooltip(Canvas canvas)
    {
        tooltip = NewRect("StatusTooltip", canvas.transform);
        tooltip.pivot = new Vector2(0, 1);
        tooltip.sizeDelta = new Vector2(430, 150);
        var overlay = tooltip.gameObject.AddComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingLayerID = canvas.sortingLayerID;
        //손패 Canvas_Overlay(20) 앞, 팝업 Canvas_Popup(30) 뒤
        overlay.sortingOrder = 25;
        var image = tooltip.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.08f, 0.09f, 0.13f, 0.98f);
        image.raycastTarget = false;
        tooltipText = NewText(tooltip, 21);
        tooltipText.alignment = TextAlignmentOptions.TopLeft;
        tooltipText.margin = new Vector4(16, 12, 16, 12);
        tooltip.gameObject.SetActive(false);
    }

    //아이콘 아래 설명 표시 및 화면 경계 보정
    public void ShowTooltip(BattleStatusItemView item, string title, string description, string amount)
    {
        tooltipOwner = item;
        tooltipText.text = $"<size=26>{title}  {amount}</size>\n{description}";
        tooltip.gameObject.SetActive(true);
        tooltip.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        RectTransform parent = (RectTransform)tooltip.parent;
        Canvas canvas = parent.GetComponent<Canvas>();
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        ((RectTransform)item.transform).GetWorldCorners(corners);
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, camera, out Vector2 position);
        position.y -= 8;
        Rect bounds = parent.rect;
        if (position.y - tooltip.rect.height < bounds.yMin)
        {
            screen = RectTransformUtility.WorldToScreenPoint(camera, corners[1]);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, camera, out position);
            position.y += tooltip.rect.height + 8;
        }
        position.x = Mathf.Clamp(position.x, bounds.xMin, Mathf.Max(bounds.xMin, bounds.xMax - tooltip.rect.width));
        position.y = Mathf.Clamp(position.y, Mathf.Min(bounds.yMax, bounds.yMin + tooltip.rect.height), bounds.yMax);
        tooltip.localPosition = new Vector3(position.x, position.y, 0);
    }

    //해당 아이콘이 소유한 설명만 숨김
    public void HideTooltip(BattleStatusItemView item)
    {
        if (tooltipOwner == item) HideTooltip();
    }

    //화면 종료 시 설명 숨김
    public void HideTooltip()
    {
        tooltipOwner = null;
        if (tooltip != null) tooltip.gameObject.SetActive(false);
    }

    //바 비활성화 시 외부 설명창 정리
    private void OnDisable() => HideTooltip();

    //유닛 제거 시 캔버스 아래 설명창 제거
    private void OnDestroy()
    {
        if (tooltip != null) Destroy(tooltip.gameObject);
    }

    //부모 기준 UI 사각형 생성
    private static RectTransform NewRect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    //기존 전투 폰트로 비상호작용 텍스트 생성
    private TMP_Text NewText(RectTransform parent, float size)
    {
        RectTransform rect = NewRect("Text", parent);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }
}
