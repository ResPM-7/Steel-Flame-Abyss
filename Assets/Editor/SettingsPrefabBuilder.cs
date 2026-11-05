#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

//환경 설정 프리팹 화면 구성
public static class SettingsPrefabBuilder
{
    private const string PrefabPath = "Assets/02.Prefab/Canvas/SettingBackPanel.prefab";
    private const string PanelSpritePath = "Assets/04.UI/Img/Panels_09.png";
    private const string ButtonSpritePath = "Assets/04.UI/Img/Gold Btn C_01.png";
    private const string FontPath = "Assets/04.UI/Fonts/Pretendard-Bold SDF-Static.asset";

    [MenuItem("Tools/Steel Flame Abyss/설정 팝업 디자인 적용")]
    public static void Build()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            BuildPrefab(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[환경 설정] 설정 팝업 디자인 적용 완료");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void BuildPrefab(GameObject root)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSpritePath);
        Sprite buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonSpritePath);

        UnityEngine.UI.Image dim = root.GetComponent<UnityEngine.UI.Image>();
        dim.color = new Color(0.025f, 0.02f, 0.04f, 0.78f);

        RectTransform panel = root.transform.Find("SettingPanel") as RectTransform;
        panel.sizeDelta = new Vector2(1040f, 720f);
        UnityEngine.UI.Image panelImage = panel.GetComponent<UnityEngine.UI.Image>();
        panelImage.sprite = panelSprite;
        panelImage.type = UnityEngine.UI.Image.Type.Sliced;
        panelImage.color = new Color(0.16f, 0.13f, 0.17f, 1f);

        Transform oldGenerated = panel.Find("GeneratedSettingsContent");
        if (oldGenerated != null) Object.DestroyImmediate(oldGenerated.gameObject);
        RectTransform content = CreateRect("GeneratedSettingsContent", panel, Vector2.zero, panel.sizeDelta);

        UnityEngine.UI.Slider master = panel.Find("MasterSound")?.GetComponent<UnityEngine.UI.Slider>();
        if (master == null) throw new MissingReferenceException("SettingPanel/MasterSound 슬라이더 확인");
        ConfigureSlider(master, new Vector2(-210f, 120f), new Vector2(300f, 42f));
        UnityEngine.UI.Slider bgm = CloneSlider(master, "BgmSound", panel, new Vector2(-210f, -5f));
        UnityEngine.UI.Slider sfx = CloneSlider(master, "SfxSound", panel, new Vector2(-210f, -130f));

        AddText(content, "Title", "환경 설정", new Vector2(0f, 295f), new Vector2(500f, 70f), 40f, font, TextAlignmentOptions.Center);
        AddText(content, "AudioTitle", "음향", new Vector2(-260f, 220f), new Vector2(300f, 55f), 28f, font, TextAlignmentOptions.Center);
        AddText(content, "DisplayTitle", "화면", new Vector2(260f, 220f), new Vector2(300f, 55f), 28f, font, TextAlignmentOptions.Center);
        AddLine(content, "CenterDivider", new Vector2(0f, -5f), new Vector2(2f, 455f), new Color(0.63f, 0.49f, 0.27f, 0.8f));

        AddText(content, "MasterLabel", "전체 음량", new Vector2(-405f, 120f), new Vector2(155f, 42f), 23f, font, TextAlignmentOptions.Left);
        AddText(content, "BgmLabel", "배경음", new Vector2(-405f, -5f), new Vector2(155f, 42f), 23f, font, TextAlignmentOptions.Left);
        AddText(content, "SfxLabel", "효과음", new Vector2(-405f, -130f), new Vector2(155f, 42f), 23f, font, TextAlignmentOptions.Left);
        TMP_Text masterValue = AddText(content, "MasterValue", "100", new Vector2(-20f, 120f), new Vector2(70f, 42f), 22f, font, TextAlignmentOptions.Center);
        TMP_Text bgmValue = AddText(content, "BgmValue", "100", new Vector2(-20f, -5f), new Vector2(70f, 42f), 22f, font, TextAlignmentOptions.Center);
        TMP_Text sfxValue = AddText(content, "SfxValue", "100", new Vector2(-20f, -130f), new Vector2(70f, 42f), 22f, font, TextAlignmentOptions.Center);

        AddText(content, "ResolutionLabel", "해상도", new Vector2(260f, 145f), new Vector2(350f, 45f), 23f, font, TextAlignmentOptions.Center);
        TMP_Text resolutionValue = AddText(content, "ResolutionValue", "1920 × 1080", new Vector2(260f, 80f), new Vector2(250f, 50f), 24f, font, TextAlignmentOptions.Center);
        UnityEngine.UI.Button resolutionPrevious = AddButton(content, "ResolutionPrevious", "‹", new Vector2(95f, 80f), new Vector2(62f, 52f), 34f, font, buttonSprite);
        UnityEngine.UI.Button resolutionNext = AddButton(content, "ResolutionNext", "›", new Vector2(425f, 80f), new Vector2(62f, 52f), 34f, font, buttonSprite);

        AddText(content, "ScreenModeLabel", "화면 모드", new Vector2(260f, -25f), new Vector2(350f, 45f), 23f, font, TextAlignmentOptions.Center);
        TMP_Text screenModeValue = AddText(content, "ScreenModeValue", "전체 화면", new Vector2(260f, -90f), new Vector2(250f, 50f), 24f, font, TextAlignmentOptions.Center);
        UnityEngine.UI.Button screenModePrevious = AddButton(content, "ScreenModePrevious", "‹", new Vector2(95f, -90f), new Vector2(62f, 52f), 34f, font, buttonSprite);
        UnityEngine.UI.Button screenModeNext = AddButton(content, "ScreenModeNext", "›", new Vector2(425f, -90f), new Vector2(62f, 52f), 34f, font, buttonSprite);

        AddText(content, "ApplyHint", "변경 내용은 즉시 적용됩니다", new Vector2(0f, -275f), new Vector2(560f, 45f), 19f, font, TextAlignmentOptions.Center, new Color(0.78f, 0.72f, 0.64f));
        UnityEngine.UI.Button close = AddButton(content, "CloseButton", "×", new Vector2(445f, 292f), new Vector2(58f, 58f), 36f, font, buttonSprite);

        SettingsPanelView view = root.GetComponent<SettingsPanelView>();
        if (view == null) view = root.AddComponent<SettingsPanelView>();
        SerializedObject serialized = new SerializedObject(view);
        Set(serialized, "masterSlider", master);
        Set(serialized, "bgmSlider", bgm);
        Set(serialized, "sfxSlider", sfx);
        Set(serialized, "masterValue", masterValue);
        Set(serialized, "bgmValue", bgmValue);
        Set(serialized, "sfxValue", sfxValue);
        Set(serialized, "resolutionValue", resolutionValue);
        Set(serialized, "screenModeValue", screenModeValue);
        Set(serialized, "resolutionPrevious", resolutionPrevious);
        Set(serialized, "resolutionNext", resolutionNext);
        Set(serialized, "screenModePrevious", screenModePrevious);
        Set(serialized, "screenModeNext", screenModeNext);
        Set(serialized, "closeButton", close);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static UnityEngine.UI.Slider CloneSlider(UnityEngine.UI.Slider source, string name, Transform parent, Vector2 position)
    {
        GameObject clone = Object.Instantiate(source.gameObject, parent);
        clone.name = name;
        UnityEngine.UI.Slider slider = clone.GetComponent<UnityEngine.UI.Slider>();
        ConfigureSlider(slider, position, new Vector2(300f, 42f));
        return slider;
    }

    private static void ConfigureSlider(UnityEngine.UI.Slider slider, Vector2 position, Vector2 size)
    {
        RectTransform rect = slider.transform as RectTransform;
        rect.localScale = Vector3.one;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.wholeNumbers = false;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static TMP_Text AddText(Transform parent, string name, string value, Vector2 position, Vector2 size,
        float fontSize, TMP_FontAsset font, TextAlignmentOptions alignment, Color? color = null)
    {
        RectTransform rect = CreateRect(name, parent, position, size);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color ?? new Color(0.96f, 0.92f, 0.84f);
        text.raycastTarget = false;
        return text;
    }

    private static void AddLine(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = CreateRect(name, parent, position, size);
        UnityEngine.UI.Image image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static UnityEngine.UI.Button AddButton(Transform parent, string name, string label, Vector2 position,
        Vector2 size, float fontSize, TMP_FontAsset font, Sprite sprite)
    {
        RectTransform rect = CreateRect(name, parent, position, size);
        UnityEngine.UI.Image image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.type = UnityEngine.UI.Image.Type.Sliced;
        UnityEngine.UI.Button button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        AddText(rect, "Label", label, Vector2.zero, size, fontSize, font, TextAlignmentOptions.Center);
        return button;
    }

    private static void Set(SerializedObject serialized, string propertyName, Object value)
    {
        serialized.FindProperty(propertyName).objectReferenceValue = value;
    }
}
#endif
