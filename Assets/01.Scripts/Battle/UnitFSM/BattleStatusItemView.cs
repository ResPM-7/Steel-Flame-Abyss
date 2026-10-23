using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

//상태 이름과 수치 표시 및 마우스 설명 연결
public class BattleStatusItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private BattleStatusBarView owner;
    private TMP_Text amountText;
    private string title;
    private string description;
    private bool hovered;

    //상태 표시 데이터 연결
    public void Initialize(BattleStatusBarView bar, TMP_Text number, string name, string details)
    {
        owner = bar;
        amountText = number;
        title = name;
        description = details;
    }

    //수치 변경과 효과 종료 반영
    public void Refresh(int amount)
    {
        gameObject.SetActive(amount > 0);
        amountText.text = amount.ToString();
        if (hovered && amount > 0) Show();
    }

    //아이콘 진입 시 설명 표시
    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        Show();
    }

    //아이콘 이탈 시 설명 숨김
    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        owner.HideTooltip(this);
    }

    //현재 수치를 포함한 설명 전달
    private void Show() => owner.ShowTooltip(this, title, description, amountText.text);

    //효과 종료 및 화면 비활성화 시 설명 정리
    private void OnDisable()
    {
        hovered = false;
        if (owner != null) owner.HideTooltip(this);
    }
}
