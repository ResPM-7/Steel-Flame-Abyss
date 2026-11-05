using UnityEngine;

//설정 팝업 필요 시 생성하고 닫을 때 제거
public class SettingsPopupSpawner : MonoBehaviour
{
    [SerializeField] private SettingsPanelView prefab;
    [SerializeField] private Transform popupRoot;
    [Tooltip("현재 씬에서 사용하는 SceneMenuActions 연결")]
    [SerializeField] private SceneMenuActions menuActions;
    [Tooltip("타이틀 씬은 끄고 맵과 배틀 씬은 켜기")]
    [SerializeField] private bool showRunButtons;
    private SettingsPanelView instance;

    public void Show()
    {
        if (instance != null)
        {
            instance.InjectMenuActions(menuActions);
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
