using System;
using System.Collections.Generic;
using System.Linq;
using SteelFlameAbyss.Data;
using UnityEditor;
using UnityEngine;

namespace SteelFlameAbyss.Editor.Data
{
    /// <summary>ID/이름 검색, 서브에셋 편집, 시트 다운로드·업로드를 한 화면에서 제공하는 Inspector입니다.</summary>
    [CustomEditor(typeof(GameDatabase))]
    internal sealed class GameDatabaseEditor : UnityEditor.Editor
    {
        private const string AppsScriptTemplatePath =
            "Assets/Editor/Data/GoogleAppsScript/GameDatabaseUpload.gs.txt";

        private readonly string[] categoryLabels = { "전체", "카드", "캐릭터", "적", "유물" };
        private string searchText = string.Empty;
        private int categoryIndex;
        private Vector2 listScroll;
        private GameDataEntry selectedEntry;
        private UnityEditor.Editor selectedEntryEditor;
        private bool showDownloadSettings;
        private bool showUploadSettings;
        private bool isUploading;
        private GUIStyle selectedButtonStyle;

        private GUIStyle SelectedButtonStyle => selectedButtonStyle ??= new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = EditorGUIUtility.isProSkin ? Color.cyan : new Color(0f, 0.35f, 0.55f) }
        };

        private void OnDisable()
        {
            if (selectedEntryEditor != null)
                DestroyImmediate(selectedEntryEditor);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var database = (GameDatabase)target;

            DrawDataList(database);
            DrawSelectedEntry();
            DrawUploadSection(database);
            DrawDownloadSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDataList(GameDatabase database)
        {
            EditorGUILayout.LabelField("게임 데이터 검색", EditorStyles.boldLabel);
            searchText = EditorGUILayout.TextField("ID / 이름 검색", searchText);
            categoryIndex = GUILayout.Toolbar(categoryIndex, categoryLabels);

            var entries = GetAllEntries(database)
                .Where(MatchesCategory)
                .Where(MatchesSearch)
                .OrderBy(CategoryOrder)
                .ThenBy(entry => entry.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var listHeight = Mathf.Clamp(entries.Count * 23f + 4f, 72f, 360f);
            listScroll = EditorGUILayout.BeginScrollView(listScroll, "box", GUILayout.Height(listHeight));
            if (entries.Count == 0)
                EditorGUILayout.LabelField("검색 결과가 없습니다.", EditorStyles.centeredGreyMiniLabel);

            foreach (var entry in entries)
            {
                var label = $"{entry.Id}  {entry.DisplayName}  [{CategoryName(entry)}]";
                var style = selectedEntry == entry ? SelectedButtonStyle : GUI.skin.button;
                if (GUILayout.Button(label, style, GUILayout.Height(21f)))
                    SelectEntry(entry);
            }
            EditorGUILayout.EndScrollView();

            var missingCount = database.Cards.Count(entry => entry == null) +
                               database.Characters.Count(entry => entry == null) +
                               database.Enemies.Count(entry => entry == null) +
                               database.Relics.Count(entry => entry == null);
            if (missingCount > 0)
                EditorGUILayout.HelpBox($"Script 연결이 끊긴 데이터가 {missingCount}개 있습니다. " +
                                        "아래의 '원격 시트에서 다시 동기화'를 실행해 복구하세요.", MessageType.Error);
            else
                EditorGUILayout.HelpBox($"카드 {database.Cards.Count} / 캐릭터 {database.Characters.Count} / " +
                                        $"적 {database.Enemies.Count} / 유물 {database.Relics.Count}", MessageType.Info);
        }

        private void DrawSelectedEntry()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("선택한 데이터", EditorStyles.boldLabel);
            if (selectedEntry == null)
            {
                EditorGUILayout.HelpBox("위 목록에서 편집할 데이터를 선택하세요.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{selectedEntry.Id}  {selectedEntry.DisplayName}", EditorStyles.boldLabel);
            if (GUILayout.Button("프로젝트에서 표시", GUILayout.Width(120f)))
                EditorGUIUtility.PingObject(selectedEntry);
            EditorGUILayout.EndHorizontal();

            UnityEditor.Editor.CreateCachedEditor(selectedEntry, null, ref selectedEntryEditor);
            selectedEntryEditor.OnInspectorGUI();
            EditorGUILayout.EndVertical();
        }

        private void DrawUploadSection(GameDatabase database)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Google 시트 업로드", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "현재 데이터베이스의 카드·캐릭터·적 전체 목록을 ID 기준으로 시트에 갱신합니다. " +
                "Unity에서 수정한 값을 저장한 뒤 업로드하세요.", MessageType.Warning);

            using (new EditorGUI.DisabledGroupScope(isUploading))
            {
                if (GUILayout.Button(isUploading ? "업로드 중..." : "변경 내용 시트로 업로드", GUILayout.Height(32f)))
                    ConfirmAndUpload(database);
            }

            showUploadSettings = EditorGUILayout.Foldout(showUploadSettings, "시트 업로드 연결 설정", true);
            if (!showUploadSettings)
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sheetUploadUrl"),
                new GUIContent("Apps Script 웹앱 URL"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cardsSheetName"), new GUIContent("카드 탭 이름"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("charactersSheetName"), new GUIContent("캐릭터 탭 이름"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enemiesSheetName"), new GUIContent("적 탭 이름"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("relicsSheetName"), new GUIContent("유물 탭 이름"));

            var token = GameDatabaseSheetUpload.UploadToken;
            var changedToken = EditorGUILayout.PasswordField("업로드 토큰 (선택)", token);
            if (!string.Equals(token, changedToken, StringComparison.Ordinal))
                GameDatabaseSheetUpload.UploadToken = changedToken;

            if (GUILayout.Button("Google Apps Script 템플릿 표시"))
            {
                var template = AssetDatabase.LoadAssetAtPath<TextAsset>(AppsScriptTemplatePath);
                Selection.activeObject = template;
                EditorGUIUtility.PingObject(template);
            }
            EditorGUILayout.HelpBox(
                "템플릿을 Google Sheets의 확장 프로그램 > Apps Script에 붙여 넣고 웹 앱으로 배포한 뒤, " +
                "배포 URL을 위에 입력하세요. 토큰은 프로젝트 파일이 아닌 이 PC의 EditorPrefs에 저장됩니다.",
                MessageType.Info);
            EditorGUI.indentLevel--;
        }

        private void DrawDownloadSection()
        {
            EditorGUILayout.Space();
            showDownloadSettings = EditorGUILayout.Foldout(showDownloadSettings, "원격 CSV 다운로드 설정", true);
            if (showDownloadSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cardsCsvUrl"), new GUIContent("카드 시트"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("charactersCsvUrl"), new GUIContent("캐릭터 시트"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enemiesCsvUrl"), new GUIContent("적 시트"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("relicsCsvUrl"), new GUIContent("유물 시트 (선택)"));
                EditorGUI.indentLevel--;
            }

            if (GUILayout.Button("원격 시트에서 다시 동기화", GUILayout.Height(28f)))
            {
                serializedObject.ApplyModifiedProperties();
                GameDatabaseCsvSync.SyncFromMenu();
            }
        }

        private async void ConfirmAndUpload(GameDatabase database)
        {
            serializedObject.ApplyModifiedProperties();
            if (!EditorUtility.DisplayDialog("Google 시트 업로드",
                    "Unity의 현재 카드·캐릭터·적 데이터를 연결된 Google 시트에 반영할까요?\n" +
                    "같은 ID의 행은 수정되고, 없는 ID는 새 행으로 추가됩니다.", "업로드", "취소"))
                return;

            isUploading = true;
            Repaint();
            try
            {
                await GameDatabaseSheetUpload.UploadAsync(database);
                EditorUtility.DisplayDialog("시트 업로드 완료", "게임 데이터가 Google 시트에 반영되었습니다.", "확인");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[게임 데이터] 시트 업로드 실패\n{exception}");
                EditorUtility.DisplayDialog("시트 업로드 실패", exception.Message, "확인");
            }
            finally
            {
                isUploading = false;
                Repaint();
            }
        }

        private void SelectEntry(GameDataEntry entry)
        {
            selectedEntry = entry;
            if (selectedEntryEditor != null)
            {
                DestroyImmediate(selectedEntryEditor);
                selectedEntryEditor = null;
            }
        }

        private static IEnumerable<GameDataEntry> GetAllEntries(GameDatabase database) =>
            database.Cards.Cast<GameDataEntry>()
                .Concat(database.Characters)
                .Concat(database.Enemies)
                .Concat(database.Relics)
                .Where(entry => entry != null);

        private bool MatchesSearch(GameDataEntry entry)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return true;
            var query = searchText.Trim();
            return entry.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   entry.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool MatchesCategory(GameDataEntry entry) => categoryIndex switch
        {
            1 => entry is CardData,
            2 => entry is CharacterData,
            3 => entry is EnemyData,
            4 => entry is RelicData,
            _ => true
        };

        private static int CategoryOrder(GameDataEntry entry) => entry switch
        {
            CardData => 0,
            CharacterData => 1,
            EnemyData => 2,
            RelicData => 3,
            _ => 4
        };

        private static string CategoryName(GameDataEntry entry) => entry switch
        {
            CardData => "카드",
            CharacterData => "캐릭터",
            EnemyData => "적",
            RelicData => "유물",
            _ => "기타"
        };
    }
}
