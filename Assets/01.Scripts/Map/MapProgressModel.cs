//고정 경로의 지점 선택과 완료 상태 보관
public class MapProgressModel
{
    public int NodeCount { get; }
    public int NextIndex { get; private set; }
    public bool IsVisiting { get; private set; }
    public bool IsFailed { get; private set; }
    public bool IsComplete => NextIndex >= NodeCount;

    //고정 경로의 지점 개수 저장
    public MapProgressModel(int nodeCount)
    {
        if (nodeCount <= 0)
            throw new System.ArgumentOutOfRangeException(nameof(nodeCount));
        NodeCount = nodeCount;
    }

    //다음 지점의 선택 가능 여부 확인
    public bool CanEnter(int index)
    {
        return !IsFailed && !IsComplete && !IsVisiting && index == NextIndex;
    }

    //다음 지점 방문 시작
    public bool TryEnter(int index)
    {
        if (!CanEnter(index))
            return false;
        IsVisiting = true;
        return true;
    }

    //초기화 실패 지점의 선택 복구
    public void CancelEntry()
    {
        IsVisiting = false;
    }

    //방문 지점 완료와 다음 지점 개방
    public bool CompleteCurrent()
    {
        if (!IsVisiting || IsFailed || IsComplete)
            return false;
        IsVisiting = false;
        NextIndex++;
        return true;
    }

    //패배 이후 경로 진행 차단
    public void Fail()
    {
        IsFailed = true;
        IsVisiting = false;
    }
}
