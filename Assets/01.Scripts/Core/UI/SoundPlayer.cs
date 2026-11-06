using UnityEngine;

//씬 BGM과 버튼 효과음의 인스펙터 연결
public class SoundPlayer : MonoBehaviour
{
    [SerializeField] private string soundId;
    [SerializeField] private bool playOnStart;
    private SoundManager soundManager;

    //씬 또는 프리팹 생성자에게 공통 재생 관리자 받음
    public void Inject(SoundManager manager)
    {
        soundManager = manager;
    }

    //씬 시작 시 등록된 사운드 재생
    private void Start()
    {
        if (playOnStart) Play();
    }

    //버튼 이벤트에서 지정한 ID 재생
    public void Play() => PlaySound(soundId);

    //외부 이벤트에서 받은 ID 재생
    public void PlaySound(string id)
    {
        if (soundManager != null) soundManager.Play(id);
        else GameLogger.LogError("[사운드] SoundPlayer에 재생 관리자를 주입해 주세요", this);
    }

    //반복 사운드 정지
    public void Stop()
    {
        if (soundManager != null) soundManager.Stop(soundId);
    }
}
