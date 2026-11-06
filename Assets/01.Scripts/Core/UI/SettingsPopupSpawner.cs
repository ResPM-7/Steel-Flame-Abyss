using UnityEngine;

//설정 팝업 필요 시 생성하고 닫을 때 제거
public class SettingsPopupSpawner : MonoBehaviour
{
    [SerializeField] private SettingsPanelView prefab;
    [SerializeField] private Transform popupRoot;
    private SceneMenuActions menuActions;
    [Tooltip("타이틀 씬은 끄고 맵과 배틀 씬은 켜기")]
    [SerializeField] private bool showRunButtons;
    private SettingsPanelView instance;
    private SoundManager soundManager;

    //게임 세션의 사운드 관리자 받음
    public void InjectSound(SoundManager manager)
    {
        soundManager = manager;
        if (instance != null) InjectPopupSound();
    }

    //동적 팝업과 하위 버튼의 사운드 의존성 연결
    private void InjectPopupSound()
    {
        instance.InjectSound(soundManager);
        foreach (SoundPlayer player in instance.GetComponentsInChildren<SoundPlayer>(true))
            player.Inject(soundManager);
    }

    //게임 세션의 공통 메뉴 기능 받음
    public void InjectMenuActions(SceneMenuActions actions)
    {
        menuActions = actions;
        if (instance != null) instance.InjectMenuActions(actions);
    }

    //공통 메뉴 연결 확인 후 설정 팝업 표시
    public void Show()
    {
        if (menuActions == null)
        {
            GameLogger.LogError("[환경 설정] GameSession의 공통 메뉴가 주입되지 않았습니다", this);
            return;
        }
        if (instance != null)
        {
            instance.InjectMenuActions(menuActions);
            InjectPopupSound();
            instance.SetRunButtonsVisible(showRunButtons);
            instance.gameObject.SetActive(true);
            instance.transform.SetAsLastSibling();
            return;
        }
        if (prefab == null)
        {
            GameLogger.LogError("[환경 설정] 설정 팝업 프리팹을 연결해 주세요", this);
            return;
        }
        Transform parent = ResolveParent();
        instance = Instantiate(prefab, parent, false);
        instance.name = prefab.name;
        instance.InjectMenuActions(menuActions);
        InjectPopupSound();
        instance.SetRunButtonsVisible(showRunButtons);
        instance.transform.SetAsLastSibling();
        instance.Closed += Close;
        instance.gameObject.SetActive(true);
    }

    //씬의 팝업 캔버스를 우선 사용
    private Transform ResolveParent()
    {
        if (popupRoot != null) return popupRoot;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
            if (canvas.name == "Canvas_Popup") return canvas.transform;
        Canvas owner = GetComponentInParent<Canvas>();
        return owner != null ? owner.transform : transform.root;
    }

    private void Close()
    {
        if (instance == null) return;
        instance.Closed -= Close;
        Destroy(instance.gameObject);
        instance = null;
    }

    private void OnDestroy()
    {
        if (instance != null) instance.Closed -= Close;
    }
}
