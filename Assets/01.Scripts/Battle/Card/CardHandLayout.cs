using UnityEngine;
using UnityEngine.UI;

//활성 카드 슬롯 수에 맞춰 손패의 가로 간격을 변경
public class CardHandLayout : MonoBehaviour
{
    [SerializeField] private float fallbackSpacing = -100f;
    [SerializeField] private float[] spacingSettings;

    private HorizontalLayoutGroup layoutGroup;
    private RectTransform handRect;
    private int cachedActiveCount = -1;

    //필요한 UI 컴포넌트를 저장하고 현재 카드 수에 맞는 간격을 적용
    private void Awake()
    {
        CacheComponents();
        RefreshSpacing();
    }

    //컴포넌트가 활성화될 때 현재 슬롯 상태를 다시 반영
    private void OnEnable()
    {
        CacheComponents();
        RefreshSpacing();
    }

    //활성 카드 수가 바뀌었을 때만 손패 간격을 갱신
    private void LateUpdate()
    {
        var activeCount = CountActiveSlots();
        if (activeCount != cachedActiveCount)
            ApplySpacing(activeCount);
    }

    //외부에서 슬롯 상태를 바꾼 뒤 즉시 간격을 다시 계산
    public void RefreshSpacing()
    {
        CacheComponents();
        ApplySpacing(CountActiveSlots());
    }

    //HorizontalLayoutGroup과 RectTransform을 한 번 저장
    private void CacheComponents()
    {
        layoutGroup ??= GetComponent<HorizontalLayoutGroup>();
        handRect ??= transform as RectTransform;
    }

    //현재 활성화된 카드 슬롯 개수를 계산
    private int CountActiveSlots()
    {
        var activeCount = 0;
        for (var index = 0; index < transform.childCount; index++)
        {
            if (transform.GetChild(index).gameObject.activeSelf)
                activeCount++;
        }

        return activeCount;
    }

    //활성 카드 수에 등록된 간격을 HorizontalLayoutGroup에 적용
    private void ApplySpacing(int activeCount)
    {
        if (layoutGroup == null || handRect == null)
            return;

        var targetSpacing = GetSpacing(activeCount);
        cachedActiveCount = activeCount;
        if (Mathf.Approximately(layoutGroup.spacing, targetSpacing))
            return;

        layoutGroup.spacing = targetSpacing;
        LayoutRebuilder.MarkLayoutForRebuild(handRect);
    }

    //활성 카드 수에 해당하는 배열의 간격값을 반환
    private float GetSpacing(int activeCount)
    {
        if (activeCount <= 1)
            return 0f;

        var spacingIndex = activeCount - 2;
        if (spacingSettings != null && spacingIndex < spacingSettings.Length)
            return spacingSettings[spacingIndex];

        return fallbackSpacing;
    }

#if UNITY_EDITOR
    //Inspector에서 간격 설정을 바꾸면 다음 화면 갱신 때 다시 계산
    private void OnValidate()
    {
        cachedActiveCount = -1;
    }
#endif
}
