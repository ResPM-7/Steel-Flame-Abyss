using TMPro;
using UnityEngine;

//보유 코인 텍스트 표시
public class CoinView : MonoBehaviour, ICoinView
{
    [SerializeField] private TMP_Text coinText;
    public bool IsConfigured => coinText != null;

    //전달된 보유 코인을 텍스트에 반영
    public void ShowAmount(int amount)
    {
        if (coinText != null)
            coinText.text = amount.ToString();
    }
}
