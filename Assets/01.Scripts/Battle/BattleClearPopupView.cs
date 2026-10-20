using UnityEngine;

//게임 클리어 화면의 타이틀 이동과 종료 입력 연결
public class BattleClearPopupView : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button titleButton;
    [SerializeField] private UnityEngine.UI.Button exitButton;
    private SceneMenuActions menu;

    //클리어 메뉴 의존성 주입과 버튼 구독 연결
    public void Inject(SceneMenuActions actions)
    {
        Release();
        menu = actions;
        titleButton.onClick.AddListener(menu.ReturnToTitle);
        exitButton.onClick.AddListener(menu.ExitGame);
    }

    //클리어 버튼의 이전 메뉴 구독 해제
    private void Release()
    {
        if (menu == null) return;
        if (titleButton != null) titleButton.onClick.RemoveListener(menu.ReturnToTitle);
        if (exitButton != null) exitButton.onClick.RemoveListener(menu.ExitGame);
        menu = null;
    }

    //클리어 화면 제거 시 메뉴 구독 정리
    private void OnDestroy() { Release(); }
}
