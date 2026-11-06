using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using UnityEditor.AddressableAssets;

internal static class ObjectPoolSheetSync
{
    private const string DatabasePath = "Assets/03.Data/PoolDatabase.asset";
    private const string PoolPrefabFolder = "Assets/02.Prefab/ObjectPool";
    private static bool isSyncing;

    [MenuItem("Tools/데이터/오브젝트 풀 동기화", false, 110)]
    private static async void SyncFromMenu()
    {
        if (isSyncing)
        {
            EditorUtility.DisplayDialog("오브젝트 풀 동기화", "이미 동기화하고 있습니다.", "확인");
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorUtility.DisplayDialog("오브젝트 풀 동기화", "Unity 컴파일 또는 에셋 갱신이 끝난 뒤 다시 실행해 주세요.", "확인");
            return;
        }

        isSyncing = true;
        try
        {
            var urls = SheetConnectionSettings.GetPoolUrls();

            EditorUtility.DisplayProgressBar("오브젝트 풀 동기화", "두 시트를 받는 중...", 0.2f);
            var downloads = await Task.WhenAll(
                DownloadCsvAsync("ObjectPool", urls.ObjectPool),
                DownloadCsvAsync("CanvasPool", urls.CanvasPool));

            EditorUtility.DisplayProgressBar("오브젝트 풀 동기화", "데이터와 프리팹을 검증하는 중...", 0.6f);
            var objectRows = ReadRows("ObjectPool", downloads[0]);
            var canvasRows = ReadRows("CanvasPool", downloads[1]);
            ValidateUniqueNames(objectRows, canvasRows);
            var prefabs = FindPoolPrefabs();
            ValidatePrefabMatches(objectRows.Concat(canvasRows), prefabs);

            EditorUtility.DisplayProgressBar("오브젝트 풀 동기화", "PoolDatabase를 갱신하는 중...", 0.9f);
            ApplyToDatabase(objectRows, canvasRows, prefabs);

            Debug.Log($"[오브젝트 풀] 시트 동기화 완료: 일반 {objectRows.Count}개 / Canvas {canvasRows.Count}개");
            Debug.Log("씬의 Scene Scope와 Parent Bindings는 유지됩니다. 새 부모 키가 있을 때만 연결해 주세요.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[오브젝트 풀] 시트 동기화 실패.\n{exception}");
            EditorUtility.DisplayDialog("오브젝트 풀 동기화 실패",
                exception.Message, "확인");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            isSyncing = false;
        }
    }

    private static async Task<string> DownloadCsvAsync(string sheetName, string url)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidDataException($"{sheetName} CSV 주소가 올바르지 않습니다.");

        using var request = UnityWebRequest.Get(uri.AbsoluteUri);
        request.timeout = 30;
        var operation = request.SendWebRequest();
        while (!operation.isDone)
            await Task.Yield();

        if (request.result != UnityWebRequest.Result.Success)
            throw new InvalidDataException($"{sheetName} 시트를 받을 수 없습니다. ({request.responseCode}) {request.error}");

        var text = request.downloadHandler.text;
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidDataException($"{sheetName} 시트가 비어 있습니다.");
        if (text.TrimStart().StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"{sheetName} 주소가 CSV 대신 HTML을 반환했습니다. 시트 공유 권한을 확인해 주세요.");
        return text;
    }

    private static List<PoolRow> ReadRows(string sheetName, string csv)
    {
        var table = CsvTable.Parse(sheetName + " 시트", csv);
        table.Require("poolName", "scope", "size");
        bool isCanvas = sheetName == "CanvasPool";
        if (isCanvas) table.Require("parentKey");
        var rows = new List<PoolRow>();
        for (var rowIndex = 0; rowIndex < table.RowCount; rowIndex++)
        {
            if (table.IsTypeDeclarationRow(rowIndex))
                continue;

            var name = table.Get(rowIndex, "poolName");
            var sizeText = table.Get(rowIndex, "size");
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(sizeText))
                continue;
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidDataException($"{sheetName} 시트 {table.SourceLine(rowIndex)}행: poolName이 비어 있습니다.");
            if (!int.TryParse(sizeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var size) || size < 0)
                throw new InvalidDataException($"{sheetName} 시트 {table.SourceLine(rowIndex)}행: size는 0 이상의 정수여야 합니다.");
            string parentKey = isCanvas ? table.Get(rowIndex, "parentKey").Trim() : "";
            if (isCanvas && string.IsNullOrWhiteSpace(parentKey))
                throw new InvalidDataException($"{sheetName} 시트 {table.SourceLine(rowIndex)}행: parentKey가 비어 있습니다.");
            PoolScope scope = ParseScope(table.Get(rowIndex, "scope"), sheetName, table.SourceLine(rowIndex));
            rows.Add(new PoolRow(name.Trim(), size, scope, parentKey, sheetName, table.SourceLine(rowIndex)));
        }
        return rows;
    }

    //다중 선택 CSV 값을 명시적인 씬 조합으로 변환
    private static PoolScope ParseScope(string value, string sheetName, int line)
    {
        PoolScope scope = PoolScope.None;
        foreach (string token in (value ?? "").Split(new[] { ',', ';', '|', ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            switch (token.Trim())
            {
                case "Title": scope |= PoolScope.Title; break;
                case "Map": scope |= PoolScope.Map; break;
                case "Battle": scope |= PoolScope.Battle; break;
                default: throw new InvalidDataException($"{sheetName} 시트 {line}행: 알 수 없는 scope '{token}'. Title, Map, Battle만 사용해 주세요.");
            }
        }
        if (scope == PoolScope.None)
            throw new InvalidDataException($"{sheetName} 시트 {line}행: scope를 하나 이상 선택해 주세요.");
        return scope;
    }

    private static void ValidateUniqueNames(IEnumerable<PoolRow> objectRows, IEnumerable<PoolRow> canvasRows)
    {
        var names = new Dictionary<string, PoolRow>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in objectRows.Concat(canvasRows))
        {
            if (names.TryGetValue(row.Name, out var previous))
                throw new InvalidDataException($"풀 이름 '{row.Name}'이(가) 중복됩니다: " +
                                               $"{previous.SheetName} {previous.Line}행, {row.SheetName} {row.Line}행");
            names.Add(row.Name, row);
        }
    }

    private static Dictionary<string, GameObject> FindPoolPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(PoolPrefabFolder))
            throw new DirectoryNotFoundException($"오브젝트 풀 프리팹 폴더를 찾을 수 없습니다: {PoolPrefabFolder}");

        var result = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PoolPrefabFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;
            if (!result.TryAdd(prefab.name, prefab))
                throw new InvalidDataException($"'{prefab.name}' 이름의 프리팹이 {PoolPrefabFolder} 안에 둘 이상 있습니다.");
        }
        return result;
    }

    private static void ValidatePrefabMatches(IEnumerable<PoolRow> rows,
        IReadOnlyDictionary<string, GameObject> prefabs)
    {
        var missing = rows.Where(row => !prefabs.ContainsKey(row.Name)).Select(row => row.Name).ToArray();
        if (missing.Length > 0)
            throw new InvalidDataException($"{PoolPrefabFolder}에서 같은 이름의 프리팹을 찾지 못했습니다: {string.Join(", ", missing)}");
    }

    //모든 참조 검증 후 SO만 갱신하고 씬과 프리팹 연결 유지
    private static void ApplyToDatabase(IReadOnlyList<PoolRow> objectRows,
        IReadOnlyList<PoolRow> canvasRows, IReadOnlyDictionary<string, GameObject> prefabs)
    {
        var objects = objectRows.Select(row => ToDefinition(row, prefabs)).ToList();
        var canvases = canvasRows.Select(row => ToDefinition(row, prefabs)).ToList();
        var database = AssetDatabase.LoadAssetAtPath<PoolDatabase>(DatabasePath);
        if (database == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/03.Data"))
                AssetDatabase.CreateFolder("Assets", "03.Data");
            database = ScriptableObject.CreateInstance<PoolDatabase>();
            database.ReplaceFromSheet(objects, canvases);
            AssetDatabase.CreateAsset(database, DatabasePath);
        }
        else
        {
            Undo.RecordObject(database, "오브젝트 풀 시트 동기화");
            database.ReplaceFromSheet(objects, canvases);
            EditorUtility.SetDirty(database);
        }
        AssetDatabase.SaveAssetIfDirty(database);
    }

    //시트 행을 런타임 풀 데이터로 변환
    private static PoolDefinition ToDefinition(PoolRow row, IReadOnlyDictionary<string, GameObject> prefabs)
        => new PoolDefinition
        {
            poolName = row.Name,
            scope = row.Scope,
            parentKey = row.ParentKey,
            poolSize = row.Size,
            addressablePrefab = GetAddressableReference(prefabs[row.Name])
        };

    //프리팹 어드레서블 등록 검증과 GUID 참조 생성
    private static AssetReferenceGameObject GetAddressableReference(GameObject prefab)
    {
        string path = AssetDatabase.GetAssetPath(prefab);
        string guid = AssetDatabase.AssetPathToGUID(path);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null || settings.FindAssetEntry(guid) == null)
            throw new InvalidDataException($"Addressables Groups에 프리팹을 먼저 등록해 주세요: {path}");
        return new AssetReferenceGameObject(guid);
    }

    private readonly struct PoolRow
    {
        public PoolRow(string name, int size, PoolScope scope, string parentKey, string sheetName, int line)
        {
            Name = name;
            Size = size;
            Scope = scope;
            ParentKey = parentKey;
            SheetName = sheetName;
            Line = line;
        }

        public string Name { get; }
        public int Size { get; }
        public PoolScope Scope { get; }
        public string ParentKey { get; }
        public string SheetName { get; }
        public int Line { get; }
    }
}
