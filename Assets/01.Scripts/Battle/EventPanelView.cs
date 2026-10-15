using System;
using UnityEngine;

//이벤트 방문 정보와 맵 복귀 입력 전달
public class EventPanelView : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button continueButton;
    private Action completed;
    public int NodeIndex { get; private set; }
    public bool IsConfigured => continueButton != null;

    //현재 이벤트 지점과 완료 입력 연결
    public void Open(int nodeIndex, Action onCompleted)
    {
        NodeIndex = nodeIndex;
        completed = onCompleted;
        continueButton.onClick.RemoveListener(Complete);
        continueButton.onClick.AddListener(Complete);
        gameObject.SetActive(true);
    }

    //이벤트 완료 일회 전달
    private void Complete()
    {
        Action callback = completed;
        completed = null;
        callback?.Invoke();
    }

    //이벤트 종료 시 방문 콜백과 버튼 연결 해제
    private void OnDisable()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(Complete);
        completed = null;
    }
}
