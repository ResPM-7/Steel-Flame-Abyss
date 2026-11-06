using UnityEngine;

//게임 세션에 종속된 공통 사운드 재생과 음량 적용
public class SoundManager : MonoBehaviour
{
    private SoundLibrary library;
    private AudioSource bgm;
    private SoundEntry bgmEntry;
    private AudioSource[] effects;
    private SoundEntry[] effectEntries;
    private int nextEffect;
    private bool started;
    private bool masterInMixer;
    private bool bgmInMixer;
    private bool sfxInMixer;

    //세션에서 사운드 데이터 받음과 재생 소스 준비
    public void Inject(SoundLibrary data)
    {
        if (data == null) throw new System.ArgumentNullException(nameof(data));
        if (library == data) return;
        if (library != null) throw new System.InvalidOperationException("사운드 라이브러리는 한 번만 주입해 주세요");
        library = data;
        bgm = CreateSource("BGM", library.BgmGroup);
        effects = new AudioSource[library.SfxPoolSize];
        effectEntries = new SoundEntry[effects.Length];
        for (int i = 0; i < effects.Length; i++)
            effects[i] = CreateSource($"SFX_{i:00}", library.SfxGroup);
        if (started) ApplyVolumes();
    }

    //믹서 초기화 이후 저장된 음량 복원
    private void Start()
    {
        started = true;
        ApplyVolumes();
    }

    //고정 개수의 2D 재생 소스 생성
    private AudioSource CreateSource(string sourceName, UnityEngine.Audio.AudioMixerGroup group)
    {
        var child = new GameObject(sourceName);
        child.transform.SetParent(transform, false);
        var source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = group;
        return source;
    }

    //분류에 따라 BGM 또는 효과음 재생
    public void Play(string id)
    {
        if (library == null || !library.TryGet(id, out SoundEntry entry) || entry.Clip == null)
        {
            GameLogger.LogWarning($"[사운드] ID 또는 클립 확인: {id}", this);
            return;
        }
        if (entry.Category == SoundCategory.BGM)
        {
            if (bgmEntry == entry && bgm.isPlaying) return;
            bgmEntry = entry;
            PlaySource(bgm, entry, bgmInMixer ? 1f : GameSettings.BgmVolume);
            return;
        }
        //동일 반복 효과음의 중첩 방지
        if (entry.Loop)
            for (int i = 0; i < effects.Length; i++)
                if (effectEntries[i] == entry && effects[i].isPlaying) return;
        int slot = -1;
        for (int i = 0; i < effects.Length; i++)
        {
            int candidate = (nextEffect + i) % effects.Length;
            if (!effects[candidate].isPlaying) { slot = candidate; break; }
        }
        //포화 시 순환 재사용으로 소스 개수 제한
        if (slot < 0) slot = nextEffect;
        nextEffect = (slot + 1) % effects.Length;
        effectEntries[slot] = entry;
        PlaySource(effects[slot], entry, sfxInMixer ? 1f : GameSettings.SfxVolume);
    }

    //이전 재생 초기화와 개별 음량 적용
    private static void PlaySource(AudioSource source, SoundEntry entry, float categoryVolume)
    {
        source.Stop();
        source.clip = entry.Clip;
        source.loop = entry.Loop;
        source.pitch = 1f;
        source.volume = entry.Volume * categoryVolume;
        source.Play();
    }

    //ID에 해당하는 BGM과 반복 효과음 정지
    public void Stop(string id)
    {
        if (library == null || !library.TryGet(id, out SoundEntry entry)) return;
        if (bgmEntry == entry) StopBgm();
        for (int i = 0; i < effects.Length; i++)
            if (effectEntries[i] == entry)
            {
                effects[i].Stop();
                effects[i].clip = null;
                effectEntries[i] = null;
            }
    }

    //현재 BGM 정지와 클립 참조 해제
    public void StopBgm()
    {
        if (bgm == null) return;
        bgm.Stop();
        bgm.clip = null;
        bgmEntry = null;
    }

    //설정창 음량을 재생 중인 소리에도 즉시 반영
    public void ApplyVolumes()
    {
        if (!started || library == null) return;
        bool routed = IsRouted(library.BgmGroup) && IsRouted(library.SfxGroup);
        masterInMixer = routed && SetMixer(library.MasterParameter, GameSettings.MasterVolume);
        bgmInMixer = IsRouted(library.BgmGroup) && SetMixer(library.BgmParameter, GameSettings.BgmVolume);
        sfxInMixer = IsRouted(library.SfxGroup) && SetMixer(library.SfxParameter, GameSettings.SfxVolume);
        AudioListener.volume = masterInMixer ? 1f : GameSettings.MasterVolume;
        if (bgmEntry != null) bgm.volume = bgmEntry.Volume * (bgmInMixer ? 1f : GameSettings.BgmVolume);
        for (int i = 0; i < effects.Length; i++)
            if (effectEntries[i] != null)
                effects[i].volume = effectEntries[i].Volume * (sfxInMixer ? 1f : GameSettings.SfxVolume);
    }

    //선택한 믹서로 출력되는 그룹 확인
    private bool IsRouted(UnityEngine.Audio.AudioMixerGroup group)
        => group != null && library.Mixer != null && group.audioMixer == library.Mixer;

    //노출된 믹서 파라미터에 데시벨 적용
    private bool SetMixer(string parameter, float volume)
    {
        if (library.Mixer == null || string.IsNullOrWhiteSpace(parameter)) return false;
        return library.Mixer.SetFloat(parameter, volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20f);
    }

}
