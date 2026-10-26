using System;
using UnityEngine;
using UnityEngine.Events;

//타이틀 캐릭터 선택 패널과 새 게임 입력 연결
public class TitleCharacterSelectView : MonoBehaviour
{
    //캐릭터 버튼과 선택 가능 여부 설정
    [Serializable]
    public class Choice
    {
        public UnityEngine.UI.Button button;
        public CharacterData character;
        [NonSerialized] public UnityAction listener;
    }

    [SerializeField] private GameObject titleButtons;
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private UnityEngine.UI.Button startButton;
    [SerializeField] private GameDatabase database;
    [SerializeField] private Choice[] choices;
    private GameSession session;
    private bool starting;

    //초기 화면과 버튼 콜백 준비
    private void Awake()
    {
        selectionPanel.SetActive(false);
        titleButtons.SetActive(true);
        startButton.onClick.AddListener(ShowSelection);
        foreach (var choice in choices)
        {
            var captured = choice;
            choice.listener = () => Select(captured);
            choice.button.onClick.AddListener(choice.listener);
        }
        RefreshButtons();
    }

    //씬 세션 주입과 선택 입력 준비
    public void Inject(GameSession value)
    {
        session = value;
        RefreshButtons();
    }

    //타이틀 버튼 숨김과 캐릭터 선택 패널 표시
    public void ShowSelection()
    {
        if (session == null || starting || session.IsLoading) return;
        titleButtons.SetActive(false);
        selectionPanel.SetActive(true);
        RefreshButtons();
    }

    //사용 가능한 캐릭터로 새 게임 요청과 중복 입력 차단
    private void Select(Choice choice)
    {
        if (session == null || starting || session.IsLoading || !selectionPanel.activeInHierarchy ||
             choice.character == null) return;
        starting = true;
        RefreshButtons();
        if (session.TryStartNewGame(choice.character, database)) return;
        starting = false;
        RefreshButtons();
    }

    //미구현 캐릭터와 로딩 중 버튼 비활성화
    private void RefreshButtons()
    {
        bool ready = session != null && !starting && !session.IsLoading;
        startButton.interactable = ready;
        foreach (var choice in choices)
            choice.button.interactable = ready && choice.character != null;
    }

    //씬 종료 시 등록한 버튼 콜백 해제
    private void OnDestroy()
    {
        if (startButton != null) startButton.onClick.RemoveListener(ShowSelection);
        if (choices == null) return;
        foreach (var choice in choices)
            if (choice.button != null && choice.listener != null)
                choice.button.onClick.RemoveListener(choice.listener);
    }
}
