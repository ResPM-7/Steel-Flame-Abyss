using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SteelFlameAbyss.Editor.Data
{
    internal static class SheetConnectionSettings
    {
        private const string KeyPrefix = "SteelFlameAbyss.SheetCsv.";
        private const string LegacyDatabasePath = "Assets/03.Data/GameDatabase.asset";

        public static string CardsUrl { get => Get("Cards"); set => Set("Cards", value); }
        public static string CharactersUrl { get => Get("Characters"); set => Set("Characters", value); }
        public static string EnemiesUrl { get => Get("Enemies"); set => Set("Enemies", value); }
        public static string RelicsUrl { get => Get("Relics"); set => Set("Relics", value); }
        public static string ObjectPoolUrl { get => Get("ObjectPool"); set => Set("ObjectPool", value); }
        public static string CanvasPoolUrl { get => Get("CanvasPool"); set => Set("CanvasPool", value); }

        public static void MigrateLegacyAssetValues()
        {
            if (!File.Exists(LegacyDatabasePath))
                return;

            var lines = File.ReadAllLines(LegacyDatabasePath);
            MigrateIfEmpty("Cards", CardsUrl, "cardsCsvUrl", lines);
            MigrateIfEmpty("Characters", CharactersUrl, "charactersCsvUrl", lines);
            MigrateIfEmpty("Enemies", EnemiesUrl, "enemiesCsvUrl", lines);
            MigrateIfEmpty("Relics", RelicsUrl, "relicsCsvUrl", lines);
            MigrateIfEmpty("ObjectPool", ObjectPoolUrl, "objectPoolCsvUrl", lines);
            MigrateIfEmpty("CanvasPool", CanvasPoolUrl, "canvasPoolCsvUrl", lines);
        }

        public static DatabaseUrls GetDatabaseUrls()
        {
            var urls = new DatabaseUrls(
                NormalizeCsvUrl(CardsUrl),
                NormalizeCsvUrl(CharactersUrl),
                NormalizeCsvUrl(EnemiesUrl),
                NormalizeCsvUrl(RelicsUrl));
            ValidateRequired("카드", urls.Cards);
            ValidateRequired("캐릭터", urls.Characters);
            ValidateRequired("적", urls.Enemies);
            ValidateOptional("유물", urls.Relics);
            return urls;
        }

        public static PoolUrls GetPoolUrls()
        {
            var urls = new PoolUrls(NormalizeCsvUrl(ObjectPoolUrl), NormalizeCsvUrl(CanvasPoolUrl));
            ValidateRequired("ObjectPool", urls.ObjectPool);
            ValidateRequired("CanvasPool", urls.CanvasPool);
            return urls;
        }

        public static string NormalizeCsvUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            var normalized = url.Trim().Replace("/pubhtml?", "/pub?", StringComparison.OrdinalIgnoreCase);
            if (!normalized.Contains("output=csv", StringComparison.OrdinalIgnoreCase) &&
                !normalized.Contains("format=csv", StringComparison.OrdinalIgnoreCase))
                normalized += normalized.Contains('?') ? "&output=csv" : "?output=csv";
            return normalized;
        }

        private static void ValidateRequired(string sheetName, string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidDataException($"{sheetName} CSV 주소가 비어 있습니다. Tools > 데이터 > 시트 연결 설정에서 입력해 주세요.");
            ValidateOptional(sheetName, url);
        }

        private static void ValidateOptional(string sheetName, string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new InvalidDataException($"{sheetName} CSV 주소는 http 또는 https 주소여야 합니다.");
        }

        private static string Get(string name) => EditorPrefs.GetString(KeyPrefix + name, string.Empty);
        private static void Set(string name, string value) => EditorPrefs.SetString(KeyPrefix + name, value?.Trim() ?? string.Empty);

        private static void MigrateIfEmpty(string preferenceName, string currentValue, string yamlField,
            string[] lines)
        {
            if (!string.IsNullOrWhiteSpace(currentValue))
                return;

            var prefix = "  " + yamlField + ":";
            foreach (var line in lines)
            {
                if (!line.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                var value = line.Substring(prefix.Length).Trim();
                if (!string.IsNullOrWhiteSpace(value))
                    Set(preferenceName, value);
                return;
            }
        }

        internal readonly struct DatabaseUrls
        {
            public DatabaseUrls(string cards, string characters, string enemies, string relics)
            {
                Cards = cards;
                Characters = characters;
                Enemies = enemies;
                Relics = relics;
            }

            public string Cards { get; }
            public string Characters { get; }
            public string Enemies { get; }
            public string Relics { get; }
        }

        internal readonly struct PoolUrls
        {
            public PoolUrls(string objectPool, string canvasPool)
            {
                ObjectPool = objectPool;
                CanvasPool = canvasPool;
            }

            public string ObjectPool { get; }
            public string CanvasPool { get; }
        }
    }

    internal sealed class SheetConnectionSettingsWindow : EditorWindow
    {
        private string cardsUrl;
        private string charactersUrl;
        private string enemiesUrl;
        private string relicsUrl;
        private string objectPoolUrl;
        private string canvasPoolUrl;

        [MenuItem("Tools/시트/시트 연결 설정", false, 100)]
        public static void Open()
        {
            var window = GetWindow<SheetConnectionSettingsWindow>(true, "시트 연결 설정");
            window.minSize = new Vector2(720f, 310f);
            window.Show();
        }

        private void OnEnable()
        {
            SheetConnectionSettings.MigrateLegacyAssetValues();
            cardsUrl = SheetConnectionSettings.CardsUrl;
            charactersUrl = SheetConnectionSettings.CharactersUrl;
            enemiesUrl = SheetConnectionSettings.EnemiesUrl;
            relicsUrl = SheetConnectionSettings.RelicsUrl;
            objectPoolUrl = SheetConnectionSettings.ObjectPoolUrl;
            canvasPoolUrl = SheetConnectionSettings.CanvasPoolUrl;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("원격 CSV 연결", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "주소는 이 PC의 Unity EditorPrefs에만 저장되며 프로젝트 파일, Git, 빌드에는 포함되지 않습니다. " +
                "팀원마다 한 번씩 설정해야 합니다.", MessageType.Info);

            cardsUrl = EditorGUILayout.TextField("카드 CSV", cardsUrl);
            charactersUrl = EditorGUILayout.TextField("캐릭터 CSV", charactersUrl);
            enemiesUrl = EditorGUILayout.TextField("적 CSV", enemiesUrl);
            relicsUrl = EditorGUILayout.TextField("유물 CSV (선택)", relicsUrl);
            EditorGUILayout.Space();
            objectPoolUrl = EditorGUILayout.TextField("ObjectPool CSV", objectPoolUrl);
            canvasPoolUrl = EditorGUILayout.TextField("CanvasPool CSV", canvasPoolUrl);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("저장", GUILayout.Height(30f)))
                    Save();
                if (GUILayout.Button("취소", GUILayout.Height(30f)))
                    Close();
            }
        }

        private void Save()
        {
            SheetConnectionSettings.CardsUrl = cardsUrl;
            SheetConnectionSettings.CharactersUrl = charactersUrl;
            SheetConnectionSettings.EnemiesUrl = enemiesUrl;
            SheetConnectionSettings.RelicsUrl = relicsUrl;
            SheetConnectionSettings.ObjectPoolUrl = objectPoolUrl;
            SheetConnectionSettings.CanvasPoolUrl = canvasPoolUrl;
            // URL 필드가 제거되기 전 GameDatabase YAML에 남은 레거시 값을 Unity 직렬화로 정리합니다.
            AssetDatabase.ForceReserializeAssets(new[] { "Assets/03.Data/GameDatabase.asset" });
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("로컬 시트 연결 설정을 저장했습니다."));
        }
    }
}
