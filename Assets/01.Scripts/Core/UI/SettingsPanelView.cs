using System;
using TMPro;
using UnityEngine;

//설정 팝업 입력과 현재 값 표시
public class SettingsPanelView : MonoBehaviour
{
    [Header("음향")]
    private SoundManager soundManager;
    [SerializeField] private UnityEngine.UI.Slider masterSlider;
    [SerializeField] private UnityEngine.UI.Slider bgmSlider;
    [SerializeField] private UnityEngine.UI.Slider sfxSlider;
    [SerializeField] private TMP_Text masterValue;
    [SerializeField] private TMP_Text bgmValue;
    [SerializeField] private TMP_Text sfxValue;

    [Header("화면")]
    [SerializeField] private TMP_Text resolutionValue;
    [SerializeField] private TMP_Text screenModeValue;
    [SerializeField] private UnityEngine.UI.Button resolutionPrevious;
    [SerializeField] private UnityEngine.UI.Button resolutionNext;
    [SerializeField] private UnityEngine.UI.Button screenModePrevious;
    [SerializeField] private UnityEngine.UI.Button screenModeNext;
    [SerializeField] private UnityEngine.UI.Button closeButton;

    [Header("게임 진행 버튼")]
    [Tooltip("Title과 Exit 버튼을 묶은 부모 오브젝트")]
    [SerializeField] private GameObject runButtons;
    [SerializeField] private UnityEngine.UI.Button titleButton;
    [SerializeField] private UnityEngine.UI.Button exitButton;
    private SceneMenuActions menuActions;

    private static readonly string[] resolutions = { "1280 × 720", "1600 × 900", "1920 × 1080" };
    private static readonly string[] screenModes = { "전체 화면", "창 모드" };
    private int resolutionIndex;
    private int screenModeIndex;
    private bool isClosing;
    public event Action Closed;

    //생성자에게 씬 이동과 종료 기능 받음
    public void InjectMenuActions(SceneMenuActions actions)
    {
        menuActions = actions;
    }

    //설정 저장 후 기존 비동기 타이틀 이동 사용
    private void ReturnToTitle()
    {
        if (!CanUseMenuActions()) return;
        GameSettings.Save();
        menuActions.ReturnToTitle();
    }

    //설정 저장 후 게임 종료 또는 에디터 재생 중지
    private void ExitGame()
    {
        if (!CanUseMenuActions()) return;
        GameSettings.Save();
        menuActions.ExitGame();
    }

    //메뉴 연결 확인과 씬 이동 중 중복 입력 차단
    private bool CanUseMenuActions()
    {
        if (menuActions != null) return !menuActions.IsLoading;
        GameLogger.LogError("[환경 설정] GameSession에서 설정 팝업으로 전달된 공통 메뉴를 확인해 주세요", this);
        return false;
    }

    //팝업 생성 씬에 맞춰 Title과 Exit 버튼 표시
    public void SetRunButtonsVisible(bool visible)
    {
        if (runButtons != null) runButtons.SetActive(visible);
    }

    //팝업을 열 때 저장된 값 표시와 입력 연결
    private void OnEnable()
    {
        isClosing = false;
        resolutionIndex = GameSettings.ResolutionIndex;
        screenModeIndex = GameSettings.ScreenModeIndex;
        LoadValues();
        Bind(true);
    }

    //생성자에게 음량 적용 담당자 받음
    public void InjectSound(SoundManager manager)
    {
        soundManager = manager;
    }

    //주입된 관리자로 저장 음량 적용
    private void ApplySoundVolumes()
    {
        if (soundManager != null) soundManager.ApplyVolumes();
        else GameLogger.LogError("[환경 설정] 사운드 관리자가 주입되지 않았습니다", this);
    }

    //활성 상태에 맞춰 입력 이벤트 연결과 해제
    private void Bind(bool subscribe)
    {
        BindSlider(masterSlider, ChangeMaster, subscribe);
        BindSlider(bgmSlider, ChangeBgm, subscribe);
        BindSlider(sfxSlider, ChangeSfx, subscribe);
        BindButton(resolutionPrevious, PreviousResolution, subscribe);
        BindButton(resolutionNext, NextResolution, subscribe);
        BindButton(screenModePrevious, PreviousScreenMode, subscribe);
        BindButton(screenModeNext, NextScreenMode, subscribe);
        BindButton(closeButton, Close, subscribe);
        BindButton(titleButton, ReturnToTitle, subscribe);
        BindButton(exitButton, ExitGame, subscribe);
    }

    //이 뷰의 슬라이더 콜백만 관리
    private static void BindSlider(UnityEngine.UI.Slider slider,
        UnityEngine.Events.UnityAction<float> callback, bool subscribe)
    {
        if (slider == null) return;
        slider.onValueChanged.RemoveListener(callback);
        if (subscribe) slider.onValueChanged.AddListener(callback);
    }

    //인스펙터에 등록된 다른 버튼 이벤트 유지
    private static void BindButton(UnityEngine.UI.Button button,
        UnityEngine.Events.UnityAction callback, bool subscribe)
    {
        if (button == null) return;
        button.onClick.RemoveListener(callback);
        if (subscribe) button.onClick.AddListener(callback);
    }

    //값 변경 이벤트 없이 저장된 설정 표시
    private void LoadValues()
    {
        SetSliderWithoutNotify(masterSlider, GameSettings.MasterVolume);
        SetSliderWithoutNotify(bgmSlider, GameSettings.BgmVolume);
        SetSliderWithoutNotify(sfxSlider, GameSettings.SfxVolume);
        RefreshLabels();
    }

    //음량 슬라이더 범위를 0~1로 통일
    private static void SetSliderWithoutNotify(UnityEngine.UI.Slider slider, float value)
    {
        if (slider == null) return;
        slider.wholeNumbers = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }

    //전체 음량 적용과 표시
    private void ChangeMaster(float value)
    {
        GameSettings.SetMaster(value);
        ApplySoundVolumes();
        SetPercent(masterValue, value);
    }

    //배경음 적용과 표시
    private void ChangeBgm(float value)
    {
        GameSettings.SetBgm(value);
        ApplySoundVolumes();
        SetPercent(bgmValue, value);
    }

    //효과음 적용과 표시
    private void ChangeSfx(float value)
    {
        GameSettings.SetSfx(value);
        ApplySoundVolumes();
        SetPercent(sfxValue, value);
    }

    //화면 옵션 이전 및 다음 항목 선택
    private void PreviousResolution() => ChangeResolution(-1);
    private void NextResolution() => ChangeResolution(1);
    private void PreviousScreenMode() => ChangeScreenMode(-1);
    private void NextScreenMode() => ChangeScreenMode(1);

    //해상도 순환 선택과 적용
    private void ChangeResolution(int direction)
    {
        resolutionIndex = Wrap(resolutionIndex + direction, resolutions.Length);
        GameSettings.SetResolution(resolutionIndex);
        RefreshLabels();
    }

    //화면 모드 순환 선택과 적용
    private void ChangeScreenMode(int direction)
    {
        screenModeIndex = Wrap(screenModeIndex + direction, screenModes.Length);
        GameSettings.SetScreenMode(screenModeIndex);
        RefreshLabels();
    }

    //설정 값에 맞춰 표시 갱신
    private void RefreshLabels()
    {
        SetPercent(masterValue, masterSlider != null ? masterSlider.value : GameSettings.MasterVolume);
        SetPercent(bgmValue, bgmSlider != null ? bgmSlider.value : GameSettings.BgmVolume);
        SetPercent(sfxValue, sfxSlider != null ? sfxSlider.value : GameSettings.SfxVolume);
        if (resolutionValue != null) resolutionValue.text = resolutions[resolutionIndex];
        if (screenModeValue != null) screenModeValue.text = screenModes[screenModeIndex];
    }

    //음량을 0~100으로 표시
    private static void SetPercent(TMP_Text label, float value)
    {
        if (label != null) label.SetText("{0}", Mathf.RoundToInt(Mathf.Clamp01(value) * 100f));
    }

    //목록 양끝에서 반대쪽 항목으로 순환
    private static int Wrap(int value, int count) => (value % count + count) % count;

    //중복 닫기 방지와 생성자에게 종료 알림
    public void Close()
    {
        if (isClosing || !isActiveAndEnabled) return;
        isClosing = true;
        gameObject.SetActive(false);
        Closed?.Invoke();
    }

    //입력 해제와 변경 내용 저장
    private void OnDisable()
    {
        Bind(false);
        GameSettings.Save();
    }
}
