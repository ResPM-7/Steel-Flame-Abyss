using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

//무작위 맵 생성 설정과 가로 스크롤 노드 표시
public class RandomMapView : MonoBehaviour
{
    [Header("맵 생성")]
    [SerializeField] private RectTransform generatedRoot;
    [SerializeField] private string nodePoolName = "Node";
    private ObjectPoolManager pool;
    private bool built;
    private readonly List<MapNodeView> rentedNodes = new List<MapNodeView>();
    public bool IsConfigured => generatedRoot != null && !string.IsNullOrWhiteSpace(nodePoolName);
    [SerializeField, Range(1, 12)] private int middleFloors = 4;
    [SerializeField, Range(0f, 1f)] private float campChance = 0.2f;
    [SerializeField, Min(0)] private int maxRandomCamps = 3;
    [SerializeField] private bool useFixedSeed;
    [SerializeField] private int fixedSeed = 12345;
    [Header("노드 콘텐츠 ID")]
    [SerializeField] private int[] battleEnemyIds = { 60001 };
    [SerializeField] private int[] eventIds = { 1 };
    [SerializeField] private int bossEnemyId = 60002;
    private MapConnectionsGraphic connections;

    //씬 노드 없이 설정된 콘텐츠 후보에서 새 맵 생성
    public MapGraphData Generate()
    {
        int seed = useFixedSeed ? fixedSeed : Guid.NewGuid().GetHashCode();
        return MapGenerator.Generate(seed, middleFloors, campChance, maxRandomCamps,
            battleEnemyIds.Distinct().ToArray(), eventIds.Distinct().ToArray(), bossEnemyId);
    }

    //전투·이벤트·보스 후보의 실제 데이터 존재 확인
    public bool ValidateContent(IGameDataProvider data, Func<int, bool> hasEvent)
    {
        if (battleEnemyIds == null || battleEnemyIds.Length == 0 || eventIds == null || eventIds.Length == 0)
        {
            GameLogger.LogError("[맵] Random Map View의 전투·이벤트 ID 후보를 입력해 주세요", this);
            return false;
        }
        foreach (int id in battleEnemyIds.Concat(new[] { bossEnemyId }))
        {
            if (data.TryGetEnemy(id, out _)) continue;
            GameLogger.LogError($"[맵] 적 ID {id}를 데이터베이스에서 찾을 수 없습니다", this);
            return false;
        }
        foreach (int id in eventIds)
        {
            if (hasEvent(id)) continue;
            GameLogger.LogError($"[맵] 이벤트 ID {id}를 데이터베이스에서 찾을 수 없습니다", this);
            return false;
        }
        return true;
    }

    //저장된 맵으로 노드 버튼과 연결선 생성
    public async UniTask<MapNodeView[]> BuildAsync(MapProgressModel progress, UnityEngine.UI.ScrollRect scroll,
        ObjectPoolManager objectPool, CancellationToken cancellationToken)
    {
        if (built) throw new InvalidOperationException("맵 화면 중복 생성");
        if (!IsConfigured || objectPool == null || generatedRoot.parent != scroll.content)
            throw new InvalidOperationException("Generated Root를 Scroll View의 Content 아래에 연결해 주세요");
        built = true;
        pool = objectPool;
        try
        {
            if (!await pool.EnsurePoolReadyAsync(nodePoolName).AsUniTask().AttachExternalCancellation(cancellationToken))
                throw new InvalidOperationException($"맵 노드 풀 준비 실패: {nodePoolName}");
            cancellationToken.ThrowIfCancellationRequested();
            var content = scroll.content;
            //그래프 좌표 배치를 위해 자동 목록 크기 계산 해제
            var fitter = content.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            var layout = content.GetComponent<UnityEngine.UI.LayoutGroup>();
            if (layout != null) layout.enabled = false;
            scroll.horizontal = true;
            scroll.vertical = false;
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.sizeDelta = new Vector2(progress.Graph.FloorCount * 290f + 80f, 0f);
            content.anchoredPosition = Vector2.zero;
            generatedRoot.anchorMin = Vector2.zero;
            generatedRoot.anchorMax = Vector2.one;
            generatedRoot.pivot = new Vector2(0f, 0.5f);
            generatedRoot.sizeDelta = Vector2.zero;
            generatedRoot.anchoredPosition = Vector2.zero;

            var lineRoot = CreateRect("Routes", generatedRoot);
            lineRoot.anchorMin = Vector2.zero;
            lineRoot.anchorMax = Vector2.one;
            lineRoot.pivot = new Vector2(0f, 0.5f);
            lineRoot.sizeDelta = Vector2.zero;
            connections = lineRoot.gameObject.AddComponent<MapConnectionsGraphic>();
            lineRoot.SetAsFirstSibling();
            var positions = new Vector2[progress.NodeCount];
            var views = new MapNodeView[progress.NodeCount];
            float laneHeight = Mathf.Max(220f, Mathf.Min(440f, scroll.viewport.rect.height - 240f));
            foreach (var data in progress.Graph.Nodes)
            {
                positions[data.Id] = new Vector2(125f + data.Floor * 290f, data.Lane * laneHeight);
                var instance = pool.GetObject(nodePoolName);
                if (instance == null) throw new InvalidOperationException($"맵 노드 대여 실패: {nodePoolName}");
                if (!instance.TryGetComponent<MapNodeView>(out var view) || !view.IsConfigured)
                {
                    pool.ReturnObject(nodePoolName, instance);
                    throw new InvalidOperationException("Node 프리팹의 MapNodeView와 UI 참조 연결 필요");
                }
                rentedNodes.Add(view);
                var rect = (RectTransform)instance.transform;
                rect.SetParent(generatedRoot, false);
                rect.SetAsLastSibling();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
                rect.anchoredPosition = positions[data.Id];
                views[data.Id] = view;
                view.Configure(data);
            }
            connections.Bind(progress, positions, views.Select(view => ((RectTransform)view.transform).rect.size).ToArray());
            return views;
        }
        catch
        {
            ReleaseNodes();
            throw;
        }
    }

    //대여 노드의 연결 정리와 풀 반환
    public void ReleaseNodes()
    {
        foreach (var view in rentedNodes)
        {
            if (view == null) continue;
            view.ResetForPool();
            if (pool != null) pool.RequestReturn(nodePoolName, view.gameObject);
        }
        rentedNodes.Clear();
        if (connections != null) Destroy(connections.gameObject);
        built = false;
    }

    //씬 종료 시 대여 참조 정리
    private void OnDestroy() => ReleaseNodes();

    //방문 완료 이후 연결선 색상 갱신
    public void RefreshRoutes()
    {
        if (connections != null) connections.SetVerticesDirty();
    }

    //가로 그래프 기준 좌표 생성
    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        var rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        return rect;
    }

}
