using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EventDatabase))]
public class EventDatabaseEditor : Editor
{
    private string search = "";
    private Vector2 scroll;
    private MapEventDefinition selected;
    private Editor selectedEditor;

    //선택 이벤트 편집기 해제
    private void OnDisable()
    {
        if (selectedEditor != null) DestroyImmediate(selectedEditor);
    }

    //이벤트 목록과 선택 데이터 편집 화면 표시
    public override void OnInspectorGUI()
    {
        var database = (EventDatabase)target;
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultEvent"), new GUIContent("기본 이벤트"));
        serializedObject.ApplyModifiedProperties();
        search = EditorGUILayout.TextField("ID / 이름 검색", search);
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(300));
        foreach (var definition in database.Events)
        {
            if (definition == null) continue;
            var label = $"{definition.EventId}  {definition.DisplayName}  [{definition.GetType().Name}]";
            if (label.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (GUILayout.Button(label)) selected = definition;
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.LabelField($"등록 이벤트 {database.Events.Count}개");
        if (!database.TryValidate(out var error)) EditorGUILayout.HelpBox(error, MessageType.Warning);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("이벤트 추가")) ShowAddMenu(database);
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("선택한 이벤트", EditorStyles.boldLabel);
        if (selected == null) return;
        Editor.CreateCachedEditor(selected, null, ref selectedEditor);
        selectedEditor.OnInspectorGUI();
        if (GUILayout.Button("기본 이벤트로 지정"))
        {
            serializedObject.Update();
            serializedObject.FindProperty("defaultEvent").objectReferenceValue = selected;
            serializedObject.ApplyModifiedProperties();
        }
    }

    //생성 가능한 이벤트 종류 메뉴 표시
    private void ShowAddMenu(EventDatabase database)
    {
        var menu = new GenericMenu();
        foreach (var type in TypeCache.GetTypesDerivedFrom<MapEventDefinition>().Where(t => !t.IsAbstract && !t.ContainsGenericParameters).OrderBy(t => t.Name))
        {
            var eventType = type;
            menu.AddItem(new GUIContent(type.Name), false, () => selected = AddEvent(database, eventType));
        }
        menu.ShowAsContext();
    }

    //새 이벤트의 고유 ID 할당과 서브에셋 등록
    public static MapEventDefinition AddEvent(EventDatabase database, Type type)
    {
        if (database == null || !AssetDatabase.Contains(database)) throw new ArgumentException("저장된 데이터베이스가 필요합니다");
        if (type == null || type.IsAbstract || type.ContainsGenericParameters || !typeof(MapEventDefinition).IsAssignableFrom(type))
            throw new ArgumentException("생성 가능한 이벤트 타입이 필요합니다");
        int id = 1;
        while (database.Events.Any(e => e != null && e.EventId == id)) id++;
        var definition = (MapEventDefinition)ScriptableObject.CreateInstance(type);
        definition.name = $"{id}_{type.Name}";
        AssetDatabase.AddObjectToAsset(definition, database);
        Undo.RegisterCreatedObjectUndo(definition, "이벤트 추가");
        var entry = new SerializedObject(definition);
        entry.FindProperty("eventId").intValue = id;
        entry.FindProperty("displayName").stringValue = $"새 이벤트 {id}";
        entry.ApplyModifiedProperties();
        var data = new SerializedObject(database);
        var list = data.FindProperty("events");
        list.InsertArrayElementAtIndex(list.arraySize);
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = definition;
        if (database.DefaultEvent == null) data.FindProperty("defaultEvent").objectReferenceValue = definition;
        data.ApplyModifiedProperties();
        AssetDatabase.SaveAssetIfDirty(database);
        AssetDatabase.SaveAssetIfDirty(definition);
        return definition;
    }
}
