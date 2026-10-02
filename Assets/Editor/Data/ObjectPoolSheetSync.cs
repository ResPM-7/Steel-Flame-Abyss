using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace SteelFlameAbyss.Editor.Data
{
    internal static class ObjectPoolSheetSync
    {
        private const string ManagerPrefabPath = "Assets/02.Prefab/ObjectPoolManager.prefab";
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

                EditorUtility.DisplayProgressBar("오브젝트 풀 동기화", "ObjectPoolManager 프리팹을 갱신하는 중...", 0.9f);
                ApplyToManagerPrefab(objectRows, canvasRows, prefabs);

                Debug.Log($"[오브젝트 풀] 시트 동기화 완료: 일반 {objectRows.Count}개 / Canvas {canvasRows.Count}개");
                Debug.Log("새 Canvas 풀의 Target Canvas는 필요하면 Inspector에서 지정해 주세요.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[오브젝트 풀] 시트 동기화 실패. 기존 프리팹은 유지됩니다.\n{exception}");
                EditorUtility.DisplayDialog("오브젝트 풀 동기화 실패",
                    exception.Message + "\n\n기존 ObjectPoolManager 프리팹은 변경되지 않았습니다.", "확인");
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
            table.Require("poolName", "size");
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
                rows.Add(new PoolRow(name, size, sheetName, table.SourceLine(rowIndex)));
            }
            return rows;
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

        private static void ApplyToManagerPrefab(IReadOnlyList<PoolRow> objectRows,
            IReadOnlyList<PoolRow> canvasRows, IReadOnlyDictionary<string, GameObject> prefabs)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ManagerPrefabPath) == null)
                throw new FileNotFoundException($"ObjectPoolManager 프리팹을 찾을 수 없습니다: {ManagerPrefabPath}");

            var root = PrefabUtility.LoadPrefabContents(ManagerPrefabPath);
            try
            {
                var manager = root.GetComponent<ObjectPoolManager>();
                if (manager == null)
                    throw new InvalidDataException("ObjectPoolManager.prefab에 ObjectPoolManager 컴포넌트가 없습니다.");

                var targetCanvasByName = manager.canvasPools
                    .Where(item => !string.IsNullOrWhiteSpace(item.poolName))
                    .GroupBy(item => item.poolName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First().targetCanvas,
                        StringComparer.OrdinalIgnoreCase);

                manager.objList = objectRows.Select(row => new ObjectPoolManager.ObjectPoolItem
                {
                    poolName = row.Name,
                    prefab = prefabs[row.Name],
                    poolSize = row.Size
                }).ToList();

                manager.canvasPools = canvasRows.Select(row => new ObjectPoolManager.CanvasPoolItem
                {
                    poolName = row.Name,
                    prefab = prefabs[row.Name],
                    targetCanvas = targetCanvasByName.TryGetValue(row.Name, out var target) ? target : null,
                    poolSize = row.Size
                }).ToList();

                EditorUtility.SetDirty(manager);
                PrefabUtility.SaveAsPrefabAsset(root, ManagerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private readonly struct PoolRow
        {
            public PoolRow(string name, int size, string sheetName, int line)
            {
                Name = name;
                Size = size;
                SheetName = sheetName;
                Line = line;
            }

            public string Name { get; }
            public int Size { get; }
            public string SheetName { get; }
            public int Line { get; }
        }
    }
}
