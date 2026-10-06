using System;
using System.Collections.Generic;
using System.Linq;
using SteelFlameAbyss.Battle;
using SteelFlameAbyss.Data;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace SteelFlameAbyss.Editor.Battle
{
    /// <summary>기존 GameScene의 PlayerArea/EnemyArea를 사용하는 1:1 전투 참가자 설치 도구입니다.</summary>
    public static class BattleParticipantsSetup
    {
        private const string FontFolder = "Assets/04.UI/Fonts/";

        public static string Configure()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode에서는 설치할 수 없습니다.");
            var panel = GameObject.Find("Canvas_Dynamic/BattlePanel");
            if (panel == null) throw new InvalidOperationException("활성 BattlePanel이 필요합니다.");
            var playerArea = panel.transform.Find("PlayerArea");
            var enemyArea = panel.transform.Find("EnemyArea");
            var status = panel.transform.Find("PlayerStatusUI");
            var provider = UnityEngine.Object.FindFirstObjectByType<GameDataProvider>();
            if (playerArea == null || enemyArea == null || status == null || provider == null || !provider.IsReady)
                throw new InvalidOperationException("PlayerArea, EnemyArea, PlayerStatusUI, GameDataProvider를 먼저 연결해 주세요.");
            var font = GetFont(provider.Database);
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("전투 참가자 연결");
            var player = ConfigureView(playerArea, "PlayerImage", font, new Color(0.23f, 0.56f, 0.82f));
            var enemy = ConfigureView(enemyArea, "EnemyImage", font, new Color(0.78f, 0.29f, 0.31f));

            var statusRect = (RectTransform)status;
            Undo.RecordObject(statusRect, "플레이어 상태 위치");
            statusRect.anchorMin = statusRect.anchorMax = new Vector2(0, 1);
            statusRect.pivot = new Vector2(0, 1);
            statusRect.anchoredPosition = new Vector2(30, -30);
            statusRect.sizeDelta = new Vector2(340, 64);
            var statusBg = GetOrAdd<UnityEngine.UI.Image>(status.gameObject);
            statusBg.color = new Color(0.08f, 0.11f, 0.16f, 0.95f);
            statusBg.raycastTarget = false;
            var energy = Label(status, "EnergyText", font, Vector2.zero, new Vector2(330, 56), 26);
            energy.text = "에너지  3 / 3";

            var participants = GetOrAdd<BattleParticipants>(panel);
            var serialized = new SerializedObject(participants);
            serialized.FindProperty("dataProvider").objectReferenceValue = provider;
            serialized.FindProperty("playerView").objectReferenceValue = player;
            serialized.FindProperty("enemyView").objectReferenceValue = enemy;
            serialized.FindProperty("playerStatusLabel").objectReferenceValue = energy;
            serialized.ApplyModifiedProperties();

            // 에디터에서도 표시를 확인할 수 있지만 SO의 수치는 변경하지 않습니다.
            participants.Initialize(50001, 60001);
            EditorSceneManager.MarkSceneDirty(panel.scene);
            EditorSceneManager.SaveScene(panel.scene);
            AssetDatabase.SaveAssets();
            return "GameScene 전투 참가자 연결 완료 (50001 / 60001)";
        }

        private static BattleCombatantView ConfigureView(Transform area, string imageName,
            TMP_FontAsset font, Color color)
        {
            var picture = area.Find(imageName)?.GetComponent<UnityEngine.UI.Image>();
            if (picture == null) throw new InvalidOperationException(imageName + " Image가 없습니다.");
            var info = Rect(area, "CombatantInfo", Vector2.zero, new Vector2(420, 460));
            info.SetAsFirstSibling();
            var bg = GetOrAdd<UnityEngine.UI.Image>(info.gameObject);
            bg.color = new Color(0.07f, 0.09f, 0.14f, 0.95f);
            bg.raycastTarget = false;
            var name = Label(info, "NameText", font, new Vector2(0, 195), new Vector2(390, 45), 30);
            var detail = Label(info, "DetailText", font, new Vector2(0, 140), new Vector2(390, 65), 22);
            var hpBg = Rect(info, "HealthBar", new Vector2(0, -135), new Vector2(340, 30));
            var healthBg = GetOrAdd<UnityEngine.UI.Image>(hpBg.gameObject);
            healthBg.color = new Color(0.2f, 0.22f, 0.27f);
            healthBg.raycastTarget = false;
            var fillRect = Rect(hpBg, "Fill", Vector2.zero, new Vector2(340, 30));
            var fill = GetOrAdd<UnityEngine.UI.Image>(fillRect.gameObject);
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.color = color;
            fill.raycastTarget = false;
            var health = Label(hpBg, "HealthText", font, Vector2.zero, new Vector2(340, 30), 21);
            var stats = Label(info, "StatusText", font, new Vector2(0, -188), new Vector2(400, 60), 21);
            Undo.RecordObject(picture.rectTransform, "임시 전투 이미지 크기");
            picture.rectTransform.anchorMin = picture.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            picture.rectTransform.anchoredPosition = new Vector2(0, -5);
            picture.rectTransform.sizeDelta = new Vector2(150, 190);
            picture.raycastTarget = false;
            var view = GetOrAdd<BattleCombatantView>(area.gameObject);
            var serialized = new SerializedObject(view);
            serialized.FindProperty("portrait").objectReferenceValue = picture;
            serialized.FindProperty("healthFill").objectReferenceValue = fill;
            serialized.FindProperty("nameLabel").objectReferenceValue = name;
            serialized.FindProperty("healthLabel").objectReferenceValue = health;
            serialized.FindProperty("statusLabel").objectReferenceValue = stats;
            serialized.FindProperty("detailLabel").objectReferenceValue = detail;
            serialized.FindProperty("placeholderColor").colorValue = color;
            serialized.ApplyModifiedProperties();
            return view;
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, "전투 UI 생성");
                go.transform.SetParent(parent, false);
                child = go.transform;
            }
            var rect = (RectTransform)child;
            Undo.RecordObject(rect, "전투 UI 배치");
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static TMP_Text Label(Transform parent, string name, TMP_FontAsset font,
            Vector2 position, Vector2 size, float fontSize)
        {
            var label = GetOrAdd<TextMeshProUGUI>(Rect(parent, name, position, size).gameObject);
            label.font = font;
            label.fontSize = fontSize;
            label.enableAutoSizing = false;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.GetComponent<T>() ?? Undo.AddComponent<T>(go);

        private static TMP_FontAsset GetFont(GameDatabase database)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontFolder + "Battle Korean SDF.asset");
            var source = AssetDatabase.LoadAssetAtPath<Font>(FontFolder + "NotoSansCJKkr-Regular.otf");
            if (source == null) throw new InvalidOperationException("Noto 한글 원본 글꼴을 찾지 못했습니다.");
            if (font == null) font = MakeFont(source, "Battle Korean SDF");
            var chars = string.Concat(Enumerable.Range(32, 95).Select(i => (char)i)) +
                "방어힘약화취전투불능다음행동없음공격강특수에너지사용캐릭터분노과열정신 " +
                string.Concat(database.Characters.Select(c => c.DisplayName + c.ResourceName)) +
                string.Concat(database.Enemies.Select(e => e.DisplayName));
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            var fontSerialized = new SerializedObject(font);
            fontSerialized.FindProperty("m_SourceFontFile").objectReferenceValue = source;
            fontSerialized.ApplyModifiedPropertiesWithoutUndo();
            var newChars = new string(chars.Distinct().Where(c => !font.HasCharacter(c)).ToArray());
            if (newChars.Length > 0 && !font.TryAddCharacters(newChars, out var missing))
                throw new InvalidOperationException("글꼴에 필요한 문자가 없습니다: " + missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontFolder + "Battle Korean Dynamic.asset");
            if (fallback == null) fallback = MakeFont(source, "Battle Korean Dynamic");
            var serialized = new SerializedObject(fallback);
            serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            font.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
            if (!font.fallbackFontAssetTable.Contains(fallback)) font.fallbackFontAssetTable.Add(fallback);
            EditorUtility.SetDirty(font);
            foreach (var texture in font.atlasTextures) EditorUtility.SetDirty(texture);
            EditorUtility.SetDirty(font.material);
            AssetDatabase.SaveAssets();
            return font;
        }

        private static TMP_FontAsset MakeFont(Font source, string name)
        {
            var font = TMP_FontAsset.CreateFontAsset(source, 48, 5, GlyphRenderMode.SDFAA, 1024, 1024);
            font.name = name;
            AssetDatabase.CreateAsset(font, FontFolder + name + ".asset");
            foreach (var texture in font.atlasTextures)
            {
                texture.name = name + " Atlas";
                AssetDatabase.AddObjectToAsset(texture, font);
            }
            font.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            return font;
        }
    }
}
