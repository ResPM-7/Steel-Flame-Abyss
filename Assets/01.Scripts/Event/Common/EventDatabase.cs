using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Events/Database", fileName = "EventDatabase")]
public class EventDatabase : ScriptableObject
{
    [SerializeField] private List<MapEventDefinition> events = new List<MapEventDefinition>();
    [SerializeField] private MapEventDefinition defaultEvent;
    public IReadOnlyList<MapEventDefinition> Events => events;
    public MapEventDefinition DefaultEvent => defaultEvent;

    //이벤트 ID에 해당하는 설정 조회
    public MapEventDefinition GetEvent(int eventId)
    {
        if (TryGetEvent(eventId, out var definition)) return definition;
        throw new ArgumentException($"등록되지 않은 이벤트 ID {eventId}", nameof(eventId));
    }

    //등록된 이벤트 ID의 설정과 조회 성공 여부 반환
    public bool TryGetEvent(int eventId, out MapEventDefinition result)
    {
        result = null;
        foreach (var definition in events)
        {
            if (definition == null || definition.EventId != eventId || eventId <= 0) continue;
            if (result != null) { result = null; return false; }
            result = definition;
        }
        return result != null;
    }

    //이벤트 목록과 기본 이벤트의 유효성 검사
    public bool TryValidate(out string error)
    {
        var ids = new HashSet<int>();
        foreach (var definition in events)
        {
            if (definition == null || definition.EventId <= 0 || !ids.Add(definition.EventId))
            {
                error = "이벤트 누락 또는 잘못되거나 중복된 ID가 있습니다";
                return false;
            }
        }
        if (defaultEvent == null || !events.Contains(defaultEvent))
        {
            error = "목록에 등록된 기본 이벤트를 지정해 주세요";
            return false;
        }
        error = null;
        return true;
    }
}
