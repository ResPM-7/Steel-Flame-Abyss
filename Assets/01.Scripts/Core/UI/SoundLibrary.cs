using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

//사운드 재생 분류
public enum SoundCategory { BGM, UI, Gameplay }

//사운드 ID와 개별 재생 설정
[Serializable]
public class SoundEntry
{
    [SerializeField] private string id;
    [SerializeField] private AudioClip clip;
    [SerializeField] private SoundCategory category;
    [SerializeField] private bool loop;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    public string Id => id;
    public AudioClip Clip => clip;
    public SoundCategory Category => category;
    public bool Loop => loop;
    public float Volume => Mathf.Clamp01(volume);
}

//공통 사운드 목록과 믹서 연결 데이터
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [SerializeField] private List<SoundEntry> sounds = new List<SoundEntry>();
    [SerializeField, Min(1)] private int sfxPoolSize = 12;
    [Header("선택 사항: 믹서 미연결 시 AudioSource 음량 사용")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private string masterParameter = "MasterVolume";
    [SerializeField] private string bgmParameter = "BgmVolume";
    [SerializeField] private string sfxParameter = "SfxVolume";
    public int SfxPoolSize => Mathf.Max(1, sfxPoolSize);
    public AudioMixer Mixer => mixer;
    public AudioMixerGroup BgmGroup => bgmGroup;
    public AudioMixerGroup SfxGroup => sfxGroup;
    public string MasterParameter => masterParameter;
    public string BgmParameter => bgmParameter;
    public string SfxParameter => sfxParameter;
    private Dictionary<string, SoundEntry> lookup;

    //에셋 로드 시 검색표 준비
    private void OnEnable() => BuildLookup();
#if UNITY_EDITOR
    //인스펙터 수정 후 검색표 갱신
    private void OnValidate() => BuildLookup();
#endif
    //ID 중복과 클립 누락 확인
    private void BuildLookup()
    {
        if (lookup == null) lookup = new Dictionary<string, SoundEntry>(StringComparer.Ordinal);
        else lookup.Clear();
        foreach (SoundEntry sound in sounds)
        {
            if (sound == null || string.IsNullOrWhiteSpace(sound.Id)) continue;
            string key = sound.Id.Trim();
            if (lookup.ContainsKey(key))
            {
                GameLogger.LogWarning($"[사운드] 중복 ID: {key}", this);
                continue;
            }
            lookup.Add(key, sound);
            if (sound.Clip == null) GameLogger.LogWarning($"[사운드] 클립 누락: {key}", this);
        }
    }

    //등록된 ID로 사운드 검색
    public bool TryGet(string id, out SoundEntry sound)
    {
        sound = null;
        if (string.IsNullOrWhiteSpace(id)) return false;
        if (lookup == null) BuildLookup();
        return lookup.TryGetValue(id.Trim(), out sound);
    }
}
