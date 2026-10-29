using System.Collections.Generic;
using UnityEngine;

//생성 지점 순서에 맞춘 적 정보 UI와 데이터별 외형 생성
public class EnemyViewFactory : MonoBehaviour
{
    [SerializeField] private BattleCombatantView combatantPrefab;
    [SerializeField] private RectTransform[] enemyPoints;
    private readonly List<BattleCombatantView> spawned = new();
    public bool IsConfigured => combatantPrefab != null && enemyPoints != null &&
        enemyPoints.Length > 0 && enemyPoints[0] != null;

    //생성 전 지정 슬롯의 연결 확인
    public bool CanCreateAt(int slot) => combatantPrefab != null && enemyPoints != null &&
        slot >= 0 && slot < enemyPoints.Length && enemyPoints[slot] != null;

    //EnemyPoint1부터 다음 빈 지점에 적 생성 및 상태 연결
    public BattleCombatantView Create(EnemyData data, CombatantState state, int slot = -1)
    {
        int index = slot < 0 ? spawned.Count : slot;
        if (!IsConfigured || data == null || state == null || index >= enemyPoints.Length || enemyPoints[index] == null)
        {
            GameLogger.LogError("[적 생성] 프리팹, 적 데이터, 생성 지점 연결 확인", this);
            return null;
        }
        var view = Instantiate(combatantPrefab, enemyPoints[index], false);
        view.name = $"CombatantInfo_{data.Id}";
        var rect = (RectTransform)view.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        view.gameObject.SetActive(true);
        //적 ID 분기 없이 SO에 등록된 외형과 공통 애니메이션 연결
        view.Bind(data.DisplayName, data.Artwork, state, data.BattlePrefab, true);
        spawned.Add(view);
        return view;
    }

    //새 전투 준비 시 이전 UI와 외형 정리
    public void Clear()
    {
        foreach (var view in spawned)
        {
            if (view == null) continue;
            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
        }
        spawned.Clear();
    }
}
