using UnityEngine;

//패배 팝업의 메뉴 입력 연결
public class BattleDefeatPopupView : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button titleButton;
    [SerializeField] private UnityEngine.UI.Button exitButton;
    private SceneMenuActions menu;

    //패배 메뉴 의존성 주입과 중복 구독 방지
    public void Inject(SceneMenuActions actions)
    {
        if (menu != null)
        {
            titleButton.onClick.RemoveListener(menu.ReturnToTitle);
            exitButton.onClick.RemoveListener(menu.ExitGame);
        }
        menu = actions;
        titleButton.onClick.AddListener(menu.ReturnToTitle);
        exitButton.onClick.AddListener(menu.ExitGame);
    }
}
