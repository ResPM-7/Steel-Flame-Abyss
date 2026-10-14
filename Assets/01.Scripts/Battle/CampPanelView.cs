using System;
using TMPro;
using UnityEngine;

//캠프 휴식 입력과 회복 결과 표시
public class CampPanelView : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button restButton;
    [SerializeField] private UnityEngine.UI.Button continueButton;
    [Range(1, 100)] [SerializeField] private int healPercent = 30;
    private CampVisitModel visit;
    private Action completed;
    public bool IsConfigured => restButton != null && continueButton != null &&
        restButton != continueButton;

    //캠프 방문 데이터와 버튼 입력 연결
    public void Open(CombatantState health, Action onCompleted)
    {
        visit = new CampVisitModel(health, Mathf.Clamp(healPercent, 1, 100));
        completed = onCompleted;
        restButton.onClick.RemoveListener(Rest);
        continueButton.onClick.RemoveListener(Continue);
        restButton.onClick.AddListener(Rest);
        continueButton.onClick.AddListener(Continue);
        restButton.interactable = true;
        continueButton.interactable = false;
        gameObject.SetActive(true);
    }

    //휴식 회복과 결과 표시 및 중복 입력 차단
    private void Rest()
    {
        if (visit == null || !visit.TryRest(out int healed))
            return;
        restButton.interactable = false;
        continueButton.interactable = true;
    }

    //휴식 완료 후 맵 복귀 요청
    private void Continue()
    {
        if (visit == null || !visit.HasRested || completed == null)
            return;
        Action callback = completed;
        completed = null;
        continueButton.interactable = false;
        callback();
    }

    //캠프 종료 시 방문 정보와 버튼 구독 해제
    private void OnDisable()
    {
        if (restButton != null)
            restButton.onClick.RemoveListener(Rest);
        if (continueButton != null)
            continueButton.onClick.RemoveListener(Continue);
        visit = null;
        completed = null;
    }
}
