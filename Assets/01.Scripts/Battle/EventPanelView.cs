using System;
using UnityEngine;

//이벤트 방문 정보와 맵 복귀 입력 전달
public class EventPanelView : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button continueButton;
    [SerializeField] private UnityEngine.UI.Button choiceTemplate;
    [SerializeField] private TMPro.TMP_Text statusText;
    private readonly System.Collections.Generic.List<UnityEngine.UI.Button> choiceButtons = new System.Collections.Generic.List<UnityEngine.UI.Button>();
    private MapEventVisit visit;
    private Action completed;
    public int NodeIndex { get; private set; }
    public bool IsConfigured => continueButton != null && choiceTemplate != null && statusText != null;

    //현재 이벤트 방문과 공통 선택지 및 완료 입력 연결
    public void Open(int nodeIndex, Action onCompleted, MapEventVisit eventVisit)
    {
        NodeIndex = nodeIndex;
        completed = onCompleted;
        visit = eventVisit ?? throw new ArgumentNullException(nameof(eventVisit));
        continueButton.onClick.RemoveListener(Complete);
        continueButton.onClick.AddListener(Complete);
        gameObject.SetActive(true);
        if (choiceButtons.Count == 0) choiceButtons.Add(choiceTemplate);
        while (choiceButtons.Count < visit.ChoiceCount)
        {
            var button = Instantiate(choiceTemplate, choiceTemplate.transform.parent, false);
            button.transform.SetSiblingIndex(continueButton.transform.GetSiblingIndex());
            choiceButtons.Add(button);
        }
        for (int i = 0; i < choiceButtons.Count; i++)
        {
            int index = i;
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => SelectChoice(index));
        }
        Refresh();
    }

    //선택 규칙 실행 후 공통 화면 갱신
    private void SelectChoice(int index)
    {
        if (completed == null || visit == null || !visit.CanChoose(index)) return;
        visit.Choose(index);
        Refresh();
    }

    //이벤트 상태와 선택지 개수 및 사용 가능 상태 표시
    private void Refresh()
    {
        statusText.text = visit.Status;
        for (int i = 0; i < choiceButtons.Count; i++)
        {
            bool used = i < visit.ChoiceCount;
            choiceButtons[i].gameObject.SetActive(used);
            if (!used) continue;
            choiceButtons[i].interactable = visit.CanChoose(i);
            var label = choiceButtons[i].GetComponentInChildren<TMPro.TMP_Text>(true);
            if (label != null) label.text = visit.GetChoiceLabel(i);
        }
    }

    //이벤트 완료 일회 전달
    private void Complete()
    {
        Action callback = completed;
        completed = null;
        visit?.Leave();
        callback?.Invoke();
    }

    //이벤트 종료 시 방문 콜백과 버튼 연결 해제
    private void OnDisable()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(Complete);
        completed = null;
    }
}
