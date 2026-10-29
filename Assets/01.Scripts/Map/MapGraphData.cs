using System;
using System.Collections.Generic;
using System.Linq;

//씬 이동과 무관한 지점 배치와 연결 정보
public class MapGraphData
{
    public int Seed { get; }
    public IReadOnlyList<MapNodeData> Nodes { get; }
    public int FloorCount => Nodes.Max(node => node.Floor) + 1;

    //생성 결과를 읽기 전용으로 보관
    public MapGraphData(int seed, IEnumerable<MapNodeData> nodes)
    {
        Seed = seed;
        Nodes = Array.AsReadOnly(nodes.ToArray());
    }
}

//지점 고유 번호와 콘텐츠 ID 및 다음 경로
public class MapNodeData
{
    public int Id { get; }
    public int Floor { get; }
    public float Lane { get; }
    public MapNodeType Type { get; }
    public int Value { get; }
    public IReadOnlyList<int> NextIds { get; }

    //생성된 지점 정보 복사
    public MapNodeData(int id, int floor, float lane, MapNodeType type, int value, IEnumerable<int> nextIds)
    {
        Id = id;
        Floor = floor;
        Lane = lane;
        Type = type;
        Value = value;
        NextIds = Array.AsReadOnly(nextIds.ToArray());
    }
}

//층별 분기와 합류 및 연속 캠프 금지 규칙 적용
public static class MapGenerator
{
    //보스 직전 캠프 고정과 나머지 층 무작위 생성
    public static MapGraphData Generate(int seed, int middleFloors, float campChance, int maxCamps,
        int[] encounterIds, int[] eventIds, int bossEncounterId)
    {
        if (encounterIds == null || encounterIds.Length == 0 || eventIds == null || eventIds.Length == 0 || bossEncounterId <= 0)
            throw new ArgumentException("전투·이벤트·보스 ID 설정 필요");
        middleFloors = Math.Max(1, Math.Min(12, middleFloors));
        var random = new Random(seed);
        var floors = new List<List<int>>();
        var lanes = new List<float>();
        var next = new List<List<int>>();
        int floorCount = middleFloors + 3;
        for (int floor = 0; floor < floorCount; floor++)
        {
            int count = floor >= floorCount - 2 ? 1 : random.Next(2, 4);
            var row = new List<int>();
            for (int i = 0; i < count; i++)
            {
                row.Add(lanes.Count);
                lanes.Add(count == 1 ? 0f : (float)i / (count - 1) - 0.5f);
                next.Add(new List<int>());
            }
            floors.Add(row);
        }
        for (int floor = 0; floor < floors.Count - 1; floor++)
        {
            var from = floors[floor];
            var to = floors[floor + 1];
            //모든 지점의 진입·탈출 경로 보장
            foreach (int id in from) Link(id, Nearest(id, to, lanes), next);
            foreach (int id in to) Link(Nearest(id, from, lanes), id, next);
            //기존 선과 교차하지 않는 추가 선택지
            foreach (int source in from)
            {
                int target = to[random.Next(to.Count)];
                bool crossing = from.Any(other => next[other].Any(end =>
                    (lanes[source] - lanes[other]) * (lanes[target] - lanes[end]) < 0f));
                if (!crossing && random.NextDouble() < 0.65) Link(source, target, next);
            }
        }
        var nodes = new List<MapNodeData>();
        int camps = 0;
        for (int floor = 0; floor < floors.Count; floor++)
        foreach (int id in floors[floor])
        {
            MapNodeType type;
            if (floor == floorCount - 1) type = MapNodeType.Boss;
            else if (floor == floorCount - 2) type = MapNodeType.Camp;
            else if (floor == 0) type = MapNodeType.Battle;
            else
            {
                bool previousCamp = nodes.Any(node => node.Type == MapNodeType.Camp && node.NextIds.Contains(id));
                bool canCamp = floor < floorCount - 3 && !previousCamp && camps < maxCamps;
                if (canCamp && random.NextDouble() < campChance)
                {
                    type = MapNodeType.Camp;
                    camps++;
                }
                else
                {
                    int roll = random.Next(100);
                    type = roll < 50 ? MapNodeType.Battle : roll < 80 ? MapNodeType.Event : MapNodeType.Shop;
                }
            }
            int value = type == MapNodeType.Battle ? encounterIds[random.Next(encounterIds.Length)] :
                type == MapNodeType.Event ? eventIds[random.Next(eventIds.Length)] :
                type == MapNodeType.Boss ? bossEncounterId : 0;
            nodes.Add(new MapNodeData(id, floor, lanes[id], type, value, next[id]));
        }
        return new MapGraphData(seed, nodes);
    }

    //가장 가까운 행의 지점 선택
    private static int Nearest(int id, List<int> candidates, List<float> lanes)
        => candidates.OrderBy(candidate => Math.Abs(lanes[id] - lanes[candidate])).First();

    //중복 없는 단방향 연결
    private static void Link(int from, int to, List<List<int>> next)
    {
        if (!next[from].Contains(to)) next[from].Add(to);
    }
}
