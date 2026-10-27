using System.Collections.Generic;
using System.Linq;

//분기 경로의 현재 위치와 실제 방문 완료 지점 보관
public class MapProgressModel
{
    private readonly HashSet<int> completed = new HashSet<int>();
    private readonly HashSet<int> visitedEdges = new HashSet<int>();
    public MapGraphData Graph { get; }
    public int NodeCount => Graph.Nodes.Count;
    public int CurrentIndex { get; private set; } = -1;
    public int VisitingIndex { get; private set; } = -1;
    public bool IsVisiting => VisitingIndex >= 0;
    public bool IsFailed { get; private set; }
    public bool IsComplete => CurrentIndex >= 0 && Graph.Nodes[CurrentIndex].Type == MapNodeType.Boss;

    //새 런의 생성 맵 연결
    public MapProgressModel(MapGraphData graph)
    {
        Graph = graph ?? throw new System.ArgumentNullException(nameof(graph));
    }

    //다음 지점의 선택 가능 여부 확인
    public bool CanEnter(int index)
    {
        if (IsFailed || IsComplete || IsVisiting || index < 0 || index >= NodeCount || completed.Contains(index)) return false;
        return CurrentIndex < 0 ? Graph.Nodes[index].Floor == 0 : Graph.Nodes[CurrentIndex].NextIds.Contains(index);
    }

    //다음 지점 방문 시작
    public bool TryEnter(int index)
    {
        if (!CanEnter(index))
            return false;
        VisitingIndex = index;
        return true;
    }

    //초기화 실패 지점의 선택 복구
    public void CancelEntry()
    {
        VisitingIndex = -1;
    }

    //방문 지점 완료와 다음 지점 개방
    public bool CompleteCurrent()
    {
        if (!IsVisiting || IsFailed || IsComplete)
            return false;
        if (CurrentIndex >= 0) visitedEdges.Add(CurrentIndex * NodeCount + VisitingIndex);
        CurrentIndex = VisitingIndex;
        completed.Add(CurrentIndex);
        VisitingIndex = -1;
        return true;
    }

    //실제 방문 완료 여부 반환
    public bool IsCompleted(int index) => completed.Contains(index);

    //실제로 지나온 연결 여부 반환
    public bool IsTraversed(int from, int to) => visitedEdges.Contains(from * NodeCount + to);

    //패배 이후 경로 진행 차단
    public void Fail()
    {
        IsFailed = true;
        VisitingIndex = -1;
    }
}
