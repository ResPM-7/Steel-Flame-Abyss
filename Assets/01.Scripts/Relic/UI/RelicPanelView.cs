using System;
using System.Collections.Generic;
using UnityEngine;

//씬별 유물 아이콘을 보유 데이터와 연결하고 툴팁 하나를 재사용
public class RelicPanelView : MonoBehaviour
{
    [SerializeField] private GameDataProvider data;
    [SerializeField] private ObjectPoolManager objectPool;
    [SerializeField] private string iconPoolName = "RelicIcon";
    [SerializeField] private RelicTooltipView tooltipPrefab;
    [SerializeField] private RectTransform tooltipRoot;
    private readonly List<RelicIconView> icons = new List<RelicIconView>();
    private OwnedRelicModel model;
    private RelicTooltipView tooltip;
    private RelicIconView hovered;
    private RelicViewFactory factory;
    private int refreshVersion;

    public void Inject(OwnedRelicModel value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (model != null) model.Changed -= Refresh;
        ReturnIcons();
        if (objectPool == null || data == null)
            throw new InvalidOperationException("유물 패널의 데이터 공급자와 오브젝트 풀을 연결해 주세요");
        factory = new RelicViewFactory(objectPool, data, iconPoolName);
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

    private async void Refresh()
    {
        int version = ++refreshVersion;
        HideTooltip();
        if (model == null || factory == null || !isActiveAndEnabled) return;
        try
        {
            bool ready = await factory.PrepareAsync();
            if (this == null || version != refreshVersion || !isActiveAndEnabled) return;
            if (!ready)
            {
                GameLogger.LogError($"[유물] {iconPoolName} 풀 준비 실패", this);
                return;
            }
            int count = 0;
            foreach (int id in model.RelicIds)
            {
                if (!data.TryGetRelic(id, out RelicData relic)) continue;
                if (count == icons.Count)
                {
                    RelicIconView view = factory.Create(id, transform, this);
                    if (view == null) continue;
                    icons.Add(view);
                }
                else icons[count].Bind(relic, this);
                count++;
            }
            for (int i = icons.Count - 1; i >= count; i--)
            {
                factory.Release(icons[i]);
                icons.RemoveAt(i);
            }
        }
        catch (Exception error)
        {
            if (this != null && version == refreshVersion)
                GameLogger.LogError($"[유물] 표시 실패: {error.Message}", this);
        }
    }

    private void ReturnIcons()
    {
        ++refreshVersion;
        foreach (RelicIconView icon in icons) factory?.Release(icon);
        icons.Clear();
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
        ReturnIcons();
    }

    private void OnDestroy()
    {
        if (model != null) model.Changed -= Refresh;
        ReturnIcons();
        if (tooltip != null) Destroy(tooltip.gameObject);
    }
}
