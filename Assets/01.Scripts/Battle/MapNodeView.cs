using System;
using UnityEngine;

public enum MapNodeType { Battle, Event, Boss, Camp, Shop }

//맵 지점 종류와 선택 입력 및 진행 색상 표시
public class MapNodeView : MonoBehaviour
{
    [SerializeField] private MapNodeType nodeType;
    [SerializeField] private int enemyId = 60001;
    [SerializeField] private MapEventDefinition eventDefinition;
    [SerializeField] private UnityEngine.UI.Button button;
    [SerializeField] private UnityEngine.UI.Image icon;
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color completedColor = new Color(0.4f, 0.8f, 0.5f);

    private Action<int> selected;
    private int index;

    public MapNodeType NodeType => nodeType;
    public int EnemyId => enemyId;
    public MapEventDefinition EventDefinition => eventDefinition;
    public bool IsConfigured => button != null && icon != null;

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

    //지점 제거 시 입력 구독 해제
    private void OnDestroy()
    {
        Unbind();
    }
}
