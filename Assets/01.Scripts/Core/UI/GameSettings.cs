using UnityEngine;
using UnityEngine.Audio;

//음향과 화면 설정 저장 및 적용
public static class GameSettings
{
    private const string MasterKey = "Settings.MasterVolume";
    private const string BgmKey = "Settings.BgmVolume";
    private const string SfxKey = "Settings.SfxVolume";
    private const string ResolutionKey = "Settings.Resolution";
    private const string ScreenModeKey = "Settings.ScreenMode";

    public static float MasterVolume => PlayerPrefs.GetFloat(MasterKey, 1f);
    public static float BgmVolume => PlayerPrefs.GetFloat(BgmKey, 1f);
    public static float SfxVolume => PlayerPrefs.GetFloat(SfxKey, 1f);
    public static int ResolutionIndex => Mathf.Clamp(PlayerPrefs.GetInt(ResolutionKey, 2), 0, 2);
    public static int ScreenModeIndex => Mathf.Clamp(PlayerPrefs.GetInt(ScreenModeKey, 0), 0, 1);

    //게임 시작 시 저장된 전체 음량 복원
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RestoreMasterVolume() => AudioListener.volume = MasterVolume;

    public static void SetMaster(float value, AudioMixer mixer, string parameter)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterKey, value);
        if (!SetMixerVolume(mixer, parameter, value))
            AudioListener.volume = value;
        else
            AudioListener.volume = 1f;
    }

    public static void SetBgm(float value, AudioMixer mixer, string parameter)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(BgmKey, value);
        SetMixerVolume(mixer, parameter, value);
    }

    public static void SetSfx(float value, AudioMixer mixer, string parameter)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxKey, value);
        SetMixerVolume(mixer, parameter, value);
    }

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

    private static bool SetMixerVolume(AudioMixer mixer, string parameter, float value)
    {
        if (mixer == null || string.IsNullOrWhiteSpace(parameter)) return false;
        float decibel = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
        return mixer.SetFloat(parameter, decibel);
    }

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
