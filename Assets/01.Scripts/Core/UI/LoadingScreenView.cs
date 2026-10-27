using System;
using TMPro;
using UnityEngine;

//씬 전환 중 최상단 화면과 입력 차단 및 실패 복귀 표시
public class LoadingScreenView : MonoBehaviour
{
    private TMP_Text label;
    private UnityEngine.UI.Button returnButton;

    //세션 아래 공통 로딩 Canvas 생성
    public static LoadingScreenView Create(Transform parent)
    {
        var root = new GameObject("LoadingScreen", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        root.transform.SetParent(parent, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var view = root.AddComponent<LoadingScreenView>();
        var backdrop = new GameObject("InputBlocker", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        backdrop.transform.SetParent(root.transform, false);
        var rect = (RectTransform)backdrop.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        backdrop.GetComponent<UnityEngine.UI.Image>().color = new Color(.035f, .045f, .065f, 1);
        view.label = Text(root.transform, "Status", new Vector2(1000, 160), new Vector2(0, 20));
        var button = new GameObject("ReturnToTitle", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        button.transform.SetParent(root.transform, false);
        ((RectTransform)button.transform).sizeDelta = new Vector2(300, 72);
        ((RectTransform)button.transform).anchoredPosition = new Vector2(0, -120);
        button.GetComponent<UnityEngine.UI.Image>().color = new Color(.18f, .25f, .34f);
        view.returnButton = button.GetComponent<UnityEngine.UI.Button>();
        Text(button.transform, "Label", new Vector2(290, 65), Vector2.zero).text = "타이틀로 돌아가기";
        root.SetActive(false);
        return view;
    }

    //기존 한글 폰트 우선 사용
    private static TMP_Text Text(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TMP_Text>();
        foreach (var existing in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (existing != text && existing.font != null) { text.font = existing.font; break; }
        text.rectTransform.sizeDelta = size; text.rectTransform.anchoredPosition = position;
        text.fontSize = 30; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        text.color = new Color(.94f, .94f, .92f);
        return text;
    }

    //진행 단계 표시와 실패 버튼 숨김
    public void Show(string message)
    {
        gameObject.SetActive(true);
        label.text = message;
        returnButton.gameObject.SetActive(false);
    }

    //오류 안내와 타이틀 복귀 입력 연결
    public void ShowFailure(Action returnToTitle)
    {
        Show("게임을 준비하지 못했습니다.\n타이틀로 돌아가 다시 시작해 주세요.");
        returnButton.gameObject.SetActive(true);
        returnButton.onClick.RemoveAllListeners();
        returnButton.onClick.AddListener(() => returnToTitle());
        returnButton.Select();
    }

    //플레이 준비 완료 후 로딩 화면 숨김
    public void Hide() => gameObject.SetActive(false);
}
