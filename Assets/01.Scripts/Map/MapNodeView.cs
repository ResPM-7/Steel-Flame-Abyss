using System;
using UnityEngine;

public enum MapNodeType { Battle, Event, Boss, Camp, Shop }

//맵 지점 종류와 선택 입력 및 진행 색상 표시
public class MapNodeView : MonoBehaviour
{
    [SerializeField] private MapNodeType nodeType;
    [UnityEngine.Serialization.FormerlySerializedAs("enemyId")]
    [Tooltip("Battle와 Boss는 적 ID / Event는 이벤트 ID / Camp와 Shop은 사용하지 않음")]
    [SerializeField] private int value = 60001;
    [SerializeField] private UnityEngine.UI.Button button;
    [SerializeField] private UnityEngine.UI.Image icon;
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color completedColor = new Color(0.4f, 0.8f, 0.5f);

    private Action<int> selected;
    private int index;
    [SerializeField] private TMPro.TMP_Text label;

    //생성 지점의 데이터와 UI 참조 연결
    public void Configure(MapNodeData data)
    {
        if (!IsConfigured) throw new InvalidOperationException("Node 프리팹의 Button·Icon·Label 연결 필요");
        Unbind();
        nodeType = data.Type;
        value = data.Value;
        label.text = nodeType == MapNodeType.Battle ? "전투" : nodeType == MapNodeType.Event ? "이벤트" :
            nodeType == MapNodeType.Shop ? "상점" : nodeType == MapNodeType.Camp ? "캠프" : "보스";
    }

    public MapNodeType NodeType => nodeType;
    public int Value => value;
    public bool IsConfigured => button != null && icon != null && label != null;

    //지점 순번과 선택 콜백 연결
    public void Bind(int nodeIndex, Action<int> onSelected)
    {
        index = nodeIndex;
        selected = onSelected;
        button.onClick.RemoveListener(Select);
        button.onClick.AddListener(Select);
    }

    //지점 완료 상태와 선택 가능 상태 표시
    public void Refresh(bool available, bool completed)
    {
        button.interactable = available;
        icon.color = completed ? completedColor : available ? availableColor : lockedColor;
        if (label != null) label.color = available || completed ? Color.white : new Color(0.75f, 0.78f, 0.82f);
    }

    //선택 지점 순번 전달
    private void Select()
    {
        selected?.Invoke(index);
    }

    //지점 버튼 입력 연결 해제
    public void Unbind()
    {
        if (button != null)
            button.onClick.RemoveListener(Select);
        selected = null;
    }

    //풀 반환 전 클릭 연결과 이전 지점 표시 초기화
    public void ResetForPool()
    {
        Unbind();
        index = -1;
        value = 0;
        nodeType = default;
        if (button != null) button.interactable = false;
        if (icon != null) icon.color = lockedColor;
        if (label != null) { label.text = string.Empty; label.color = Color.white; }
    }

    //지점 제거 시 입력 구독 해제
    private void OnDestroy()
    {
        Unbind();
    }
}
