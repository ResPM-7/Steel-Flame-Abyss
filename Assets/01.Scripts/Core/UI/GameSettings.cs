using UnityEngine;

//음향과 화면 설정 저장 및 적용
public static class GameSettings
{
    private const string MasterKey = "Settings.MasterVolume";
    private const string BgmKey = "Settings.BgmVolume";
    private const string SfxKey = "Settings.SfxVolume";
    private const string ResolutionKey = "Settings.Resolution";
    private const string ScreenModeKey = "Settings.ScreenMode";

    public static float MasterVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey, 1f));
    public static float BgmVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(BgmKey, 1f));
    public static float SfxVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));
    public static int ResolutionIndex => Mathf.Clamp(PlayerPrefs.GetInt(ResolutionKey, 2), 0, 2);
    public static int ScreenModeIndex => Mathf.Clamp(PlayerPrefs.GetInt(ScreenModeKey, 0), 0, 1);

    //전체 음량 저장
    public static void SetMaster(float value) => PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value));

    //배경음 음량 저장
    public static void SetBgm(float value) => PlayerPrefs.SetFloat(BgmKey, Mathf.Clamp01(value));

    //효과음 음량 저장
    public static void SetSfx(float value) => PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));

    public static void SetResolution(int index)
    {
        index = Mathf.Clamp(index, 0, 2);
        PlayerPrefs.SetInt(ResolutionKey, index);
        ApplyDisplay(index, ScreenModeIndex);
    }

    public static void SetScreenMode(int index)
    {
        index = Mathf.Clamp(index, 0, 1);
        PlayerPrefs.SetInt(ScreenModeKey, index);
        ApplyDisplay(ResolutionIndex, index);
    }

    public static void Save() => PlayerPrefs.Save();

    private static void ApplyDisplay(int resolutionIndex, int screenModeIndex)
    {
        Vector2Int[] resolutions =
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080)
        };
        Vector2Int resolution = resolutions[Mathf.Clamp(resolutionIndex, 0, resolutions.Length - 1)];
        FullScreenMode mode = screenModeIndex == 0 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
#if !UNITY_EDITOR
        Screen.SetResolution(resolution.x, resolution.y, mode);
#endif
    }
}
