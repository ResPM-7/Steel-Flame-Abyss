using UnityEngine;
using UnityEngine.UI;

//스크롤 영역에서 잘리는 단방향 연결선과 화살촉 표시
[RequireComponent(typeof(CanvasRenderer))]
public class MapConnectionsGraphic : MaskableGraphic
{
    private MapProgressModel progress;
    private Vector2[] positions;
    private Vector2[] sizes;

    //노드 좌표와 진행 상태 연결
    public void Bind(MapProgressModel model, Vector2[] points, Vector2[] nodeSizes)
    {
        progress = model;
        positions = points;
        sizes = nodeSizes;
        raycastTarget = false;
        SetVerticesDirty();
    }

    //각 연결의 이동 방향과 방문 상태 색상 갱신
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (progress == null || positions == null) return;
        foreach (var node in progress.Graph.Nodes)
            foreach (int next in node.NextIds)
            {
                Color tint = progress.IsTraversed(node.Id, next) ? new Color(0.4f, 0.85f, 0.55f) :
                    progress.CurrentIndex == node.Id && progress.CanEnter(next) ? new Color(0.35f, 0.9f, 1f) :
                    new Color(0.42f, 0.46f, 0.52f);
                Vector2 direction = (positions[next] - positions[node.Id]).normalized;
                //노드 사각형 경계 바깥에서 연결 시작과 종료
            Vector2 start = positions[node.Id] + direction * EdgeDistance(direction, sizes[node.Id]);
            Vector2 end = positions[next] - direction * EdgeDistance(direction, sizes[next]);
                Vector2 side = new Vector2(-direction.y, direction.x);
                Vector2 stemEnd = end - direction * 14f;
                int index = mesh.currentVertCount;
                mesh.AddVert(start + side * 2f, tint, Vector2.zero);
                mesh.AddVert(start - side * 2f, tint, Vector2.zero);
                mesh.AddVert(stemEnd - side * 2f, tint, Vector2.zero);
                mesh.AddVert(stemEnd + side * 2f, tint, Vector2.zero);
                mesh.AddTriangle(index, index + 1, index + 2);
                mesh.AddTriangle(index, index + 2, index + 3);
                index = mesh.currentVertCount;
                mesh.AddVert(end, tint, Vector2.zero);
                mesh.AddVert(stemEnd + side * 8f, tint, Vector2.zero);
                mesh.AddVert(stemEnd - side * 8f, tint, Vector2.zero);
                mesh.AddTriangle(index, index + 1, index + 2);
            }
    }

    //프리팹 크기에 맞춘 연결선 여백 계산
    private static float EdgeDistance(Vector2 direction, Vector2 size)
        => 1f / Mathf.Max(Mathf.Abs(direction.x) / Mathf.Max(1f, size.x * 0.5f),
            Mathf.Abs(direction.y) / Mathf.Max(1f, size.y * 0.5f)) + 8f;
}
