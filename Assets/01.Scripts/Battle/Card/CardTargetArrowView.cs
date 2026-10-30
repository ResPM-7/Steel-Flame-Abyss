using UnityEngine;
using UnityEngine.UI;

//Canvas 좌표의 베지어 곡선과 화살촉 표시
[RequireComponent(typeof(CanvasRenderer))]
public class CardTargetArrowView : MaskableGraphic
{
    [SerializeField, Min(1f)] private float lineWidth = 6f;
    [SerializeField, Min(1f)] private float arrowSize = 22f;
    [SerializeField, Min(0f)] private float curveHeight = 120f;
    [SerializeField, Range(8, 64)] private int segments = 32;
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.75f, 0.2f);
    private Vector2 start;
    private Vector2 end;
    private bool visible;
    private bool selected;

    //입력 차단 없이 빈 그래픽으로 시작
    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    //카드 상단과 대상 앵커를 화살표 Canvas 좌표로 변환
    public void Show(RectTransform card, RectTransform anchor, Vector2 pointer, bool hasTarget)
    {
        raycastTarget = false;
        start = ToLocal(ToScreen(card, new Vector2(card.rect.center.x, card.rect.yMax)));
        end = ToLocal(anchor != null ? ToScreen(anchor, Vector2.zero) : pointer);
        selected = hasTarget;
        visible = true;
        SetVerticesDirty();
    }

    //선택 종료 시 메쉬 비움
    public void Hide()
    {
        visible = false;
        SetVerticesDirty();
    }

    //RectTransform 로컬 위치를 화면 위치로 변환
    private static Vector2 ToScreen(RectTransform rect, Vector2 point)
    {
        var owner = rect.GetComponentInParent<Canvas>();
        var camera = owner != null && owner.renderMode != RenderMode.ScreenSpaceOverlay ? owner.worldCamera : null;
        return RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(point));
    }

    //화면 위치를 현재 그래픽 로컬 위치로 변환
    private Vector2 ToLocal(Vector2 screen)
    {
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screen, camera, out var local);
        return local;
    }

    //곡선 구간별 사각형과 끝점의 삼각형 생성
    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear();
        if (!visible || (end - start).sqrMagnitude < 1f) return;
        Color32 tint = selected ? selectedColor : idleColor;
        Vector2 control = (start + end) * 0.5f + Vector2.up * curveHeight;
        //적 선택 시 끝점 바로 위에서 수직으로 내려오는 곡선 구성
        if (selected)
            control = end + Vector2.up * Mathf.Max(curveHeight, arrowSize * 2f);
        Vector2 direction = selected ? Vector2.down : (end - control).normalized;
        if (direction.sqrMagnitude < 0.01f) direction = (end - start).normalized;
        Vector2 tipBase = end - direction * arrowSize;
        Vector2 previous = start;
        int count = Mathf.Max(8, segments);
        for (int i = 1; i <= count; i++)
        {
            float t = (float)i / count;
            Vector2 next = (1f - t) * (1f - t) * start + 2f * (1f - t) * t * control + t * t * tipBase;
            Vector2 delta = (next - previous).normalized;
            Vector2 normal = new Vector2(-delta.y, delta.x) * lineWidth * 0.5f;
            int offset = helper.currentVertCount;
            helper.AddVert(previous - normal, tint, Vector2.zero);
            helper.AddVert(previous + normal, tint, Vector2.zero);
            helper.AddVert(next + normal, tint, Vector2.zero);
            helper.AddVert(next - normal, tint, Vector2.zero);
            helper.AddTriangle(offset, offset + 1, offset + 2);
            helper.AddTriangle(offset, offset + 2, offset + 3);
            previous = next;
        }
        Vector2 side = new Vector2(-direction.y, direction.x) * arrowSize * 0.55f;
        int tip = helper.currentVertCount;
        helper.AddVert(end, tint, Vector2.zero);
        helper.AddVert(tipBase - side, tint, Vector2.zero);
        helper.AddVert(tipBase + side, tint, Vector2.zero);
        helper.AddTriangle(tip, tip + 1, tip + 2);
    }
}
