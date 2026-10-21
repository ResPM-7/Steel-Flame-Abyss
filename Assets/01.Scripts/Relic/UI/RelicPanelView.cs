using System;
using System.Collections.Generic;
using UnityEngine;

//씬별 유물 아이콘을 보유 데이터와 연결하고 툴팁 하나를 재사용
public sealed class RelicPanelView : MonoBehaviour
{
    [SerializeField] private GameDataProvider data;
    [SerializeField] private RelicIconView iconPrefab;
    [SerializeField] private RelicTooltipView tooltipPrefab;
    [SerializeField] private RectTransform tooltipRoot;
    private readonly List<RelicIconView> icons = new List<RelicIconView>();
    private OwnedRelicModel model;
    private RelicTooltipView tooltip;
    private RelicIconView hovered;

    public void Inject(OwnedRelicModel value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (model != null) model.Changed -= Refresh;
        model = value;
        if (isActiveAndEnabled) model.Changed += Refresh;
        Refresh();
    }

    private void OnEnable()
    {
        if (model == null) return;
        model.Changed += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        HideTooltip();
        if (model == null || data == null || iconPrefab == null) return;
        int count = 0;
        foreach (int id in model.RelicIds)
        {
            if (!data.TryGetRelic(id, out RelicData relic)) continue;
            if (count == icons.Count) icons.Add(Instantiate(iconPrefab, transform, false));
            icons[count].Bind(relic, this);
            icons[count].gameObject.SetActive(true);
            count++;
        }
        for (int i = count; i < icons.Count; i++) icons[i].gameObject.SetActive(false);
    }

    public void ShowTooltip(RelicIconView source, RelicData relic)
    {
        if (!isActiveAndEnabled || tooltipPrefab == null || tooltipRoot == null) return;
        if (tooltip == null) tooltip = Instantiate(tooltipPrefab, tooltipRoot, false);
        hovered = source;
        tooltip.Show(relic, (RectTransform)source.transform);
    }

    public void HideTooltip(RelicIconView source = null)
    {
        if (source != null && source != hovered) return;
        hovered = null;
        if (tooltip != null) tooltip.Hide();
    }

    private void OnDisable()
    {
        if (model != null) model.Changed -= Refresh;
        HideTooltip();
    }

    private void OnDestroy()
    {
        if (model != null) model.Changed -= Refresh;
        if (tooltip != null) Destroy(tooltip.gameObject);
    }
}
