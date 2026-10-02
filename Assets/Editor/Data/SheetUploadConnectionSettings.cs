using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SteelFlameAbyss.Editor.Data
{
    internal static class SheetUploadConnectionSettings
    {
        private const string KeyPrefix = "SteelFlameAbyss.SheetUpload.";
        private const string UploadTokenKey = "SteelFlameAbyss.GameData.UploadToken";
        private const string LegacyDatabasePath = "Assets/03.Data/GameDatabase.asset";

        public static string UploadUrl { get => Get("Url"); set => Set("Url", value); }
        public static string Token
        {
            get => EditorPrefs.GetString(UploadTokenKey, string.Empty);
            set => EditorPrefs.SetString(UploadTokenKey, value ?? string.Empty);
        }
        public static string CardsSheetName { get => Get("CardsSheet", "Total Card"); set => Set("CardsSheet", value); }
        public static string CharactersSheetName { get => Get("CharactersSheet", "Character"); set => Set("CharactersSheet", value); }
        public static string EnemiesSheetName { get => Get("EnemiesSheet", "Enemy"); set => Set("EnemiesSheet", value); }
        public static string RelicsSheetName { get => Get("RelicsSheet", "Relic"); set => Set("RelicsSheet", value); }

        public static Values GetValidated()
        {
            var values = new Values(UploadUrl?.Trim(), Token, CardsSheetName?.Trim(),
                CharactersSheetName?.Trim(), EnemiesSheetName?.Trim(), RelicsSheetName?.Trim());
            if (!Uri.TryCreate(values.UploadUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidDataException("Tools > 시트 > 시트 업로드 연결에서 HTTPS Apps Script 웹앱 URL을 입력해 주세요.");
            if (string.IsNullOrWhiteSpace(values.CardsSheetName) ||
                string.IsNullOrWhiteSpace(values.CharactersSheetName) ||
                string.IsNullOrWhiteSpace(values.EnemiesSheetName))
                throw new InvalidDataException("카드, 캐릭터, 적 탭 이름은 비워 둘 수 없습니다.");
            return values;
        }

        public static void MigrateLegacyAssetValues()
        {
            if (!File.Exists(LegacyDatabasePath))
                return;

            var lines = File.ReadAllLines(LegacyDatabasePath);
            MigrateIfEmpty("Url", UploadUrl, "sheetUploadUrl", lines);
            MigrateIfEmpty("CardsSheet", CardsSheetName, "cardsSheetName", lines);
            MigrateIfEmpty("CharactersSheet", CharactersSheetName, "charactersSheetName", lines);
            MigrateIfEmpty("EnemiesSheet", EnemiesSheetName, "enemiesSheetName", lines);
            MigrateIfEmpty("RelicsSheet", RelicsSheetName, "relicsSheetName", lines);
        }

        private static string Get(string name, string defaultValue = "") =>
            EditorPrefs.GetString(KeyPrefix + name, defaultValue);

        private static void Set(string name, string value) =>
            EditorPrefs.SetString(KeyPrefix + name, value?.Trim() ?? string.Empty);

        private static void MigrateIfEmpty(string preferenceName, string currentValue, string yamlField,
            string[] lines)
        {
            if (!string.IsNullOrWhiteSpace(currentValue) &&
                EditorPrefs.HasKey(KeyPrefix + preferenceName))
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

        internal readonly struct Values
        {
            public Values(string uploadUrl, string token, string cardsSheetName,
                string charactersSheetName, string enemiesSheetName, string relicsSheetName)
            {
                UploadUrl = uploadUrl;
                Token = token;
                CardsSheetName = cardsSheetName;
                CharactersSheetName = charactersSheetName;
                EnemiesSheetName = enemiesSheetName;
                RelicsSheetName = relicsSheetName;
            }

            public string UploadUrl { get; }
            public string Token { get; }
            public string CardsSheetName { get; }
            public string CharactersSheetName { get; }
            public string EnemiesSheetName { get; }
            public string RelicsSheetName { get; }
        }
    }

    internal sealed class SheetUploadConnectionWindow : EditorWindow
    {
        private const string AppsScriptTemplatePath =
            "Assets/Editor/Data/GoogleAppsScript/GameDatabaseUpload.gs.txt";

        private string uploadUrl;
        private string token;
        private string cardsSheetName;
        private string charactersSheetName;
        private string enemiesSheetName;
        private string relicsSheetName;

        [MenuItem("Tools/시트/시트 업로드 연결", false, 110)]
        public static void Open()
        {
            var window = GetWindow<SheetUploadConnectionWindow>(true, "시트 업로드 연결");
            window.minSize = new Vector2(650f, 300f);
            window.Show();
        }

        private void OnEnable()
        {
            SheetUploadConnectionSettings.MigrateLegacyAssetValues();
            uploadUrl = SheetUploadConnectionSettings.UploadUrl;
            token = SheetUploadConnectionSettings.Token;
            cardsSheetName = SheetUploadConnectionSettings.CardsSheetName;
            charactersSheetName = SheetUploadConnectionSettings.CharactersSheetName;
            enemiesSheetName = SheetUploadConnectionSettings.EnemiesSheetName;
            relicsSheetName = SheetUploadConnectionSettings.RelicsSheetName;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Google 시트 업로드 연결", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "웹앱 URL과 토큰은 이 PC의 EditorPrefs에만 저장되며 프로젝트 파일, Git, 빌드에는 포함되지 않습니다.",
                MessageType.Info);

            uploadUrl = EditorGUILayout.TextField("Apps Script 웹앱 URL", uploadUrl);
            token = EditorGUILayout.PasswordField("업로드 토큰", token);
            EditorGUILayout.Space();
            cardsSheetName = EditorGUILayout.TextField("카드 탭 이름", cardsSheetName);
            charactersSheetName = EditorGUILayout.TextField("캐릭터 탭 이름", charactersSheetName);
            enemiesSheetName = EditorGUILayout.TextField("적 탭 이름", enemiesSheetName);
            relicsSheetName = EditorGUILayout.TextField("유물 탭 이름", relicsSheetName);

            EditorGUILayout.Space();
            if (GUILayout.Button("Google Apps Script 템플릿 표시"))
            {
                var template = AssetDatabase.LoadAssetAtPath<TextAsset>(AppsScriptTemplatePath);
                Selection.activeObject = template;
                EditorGUIUtility.PingObject(template);
            }

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
            SheetUploadConnectionSettings.UploadUrl = uploadUrl;
            SheetUploadConnectionSettings.Token = token;
            SheetUploadConnectionSettings.CardsSheetName = cardsSheetName;
            SheetUploadConnectionSettings.CharactersSheetName = charactersSheetName;
            SheetUploadConnectionSettings.EnemiesSheetName = enemiesSheetName;
            SheetUploadConnectionSettings.RelicsSheetName = relicsSheetName;
            AssetDatabase.ForceReserializeAssets(new[] { "Assets/03.Data/GameDatabase.asset" });
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("로컬 업로드 연결 설정을 저장했습니다."));
        }
    }
}
