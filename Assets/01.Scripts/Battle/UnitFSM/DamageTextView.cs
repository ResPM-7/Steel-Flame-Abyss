using DG.Tweening;
using TMPro;
using UnityEngine;

//풀에서 꺼낸 피해 숫자의 이동과 페이드 및 반환
[RequireComponent(typeof(TextMeshProUGUI))]
public class DamageTextView : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float duration = 0.6f;
    [SerializeField] private float riseHeight = 65f;
    private TextMeshProUGUI label;
    private Sequence tween;
    private ObjectPoolManager pool;
    private string poolName;

    //텍스트 참조와 입력 차단 설정
    private void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
        label.raycastTarget = false;
    }

    //화면 좌표를 풀 부모의 UI 좌표로 변환 후 연출 시작
    public void Show(ObjectPoolManager owner, string key, Vector2 screenPoint, string text, Color color)
    {
        tween?.Kill();
        pool = owner;
        poolName = key;
        var rect = (RectTransform)transform;
        var parent = (RectTransform)rect.parent;
        var canvas = GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, camera, out var point);
        rect.localPosition = new Vector3(point.x, point.y, 0);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.SetAsLastSibling();
        label.text = text;
        label.color = color;
        tween = DOTween.Sequence()
            .Append(rect.DOLocalMoveY(point.y + riseHeight, duration).SetEase(Ease.OutQuad))
            .Join(label.DOFade(0f, duration).SetEase(Ease.InQuad))
            .OnComplete(ReturnToPool);
    }

    //완료 또는 외부 비활성화 시 한 번만 반환
    private void ReturnToPool()
    {
        var owner = pool;
        pool = null;
        if (owner != null) owner.RequestReturn(poolName, gameObject);
    }

    //재대여 전 트윈과 이전 숫자 정리
    private void OnDisable()
    {
        tween?.Kill();
        tween = null;
        ReturnToPool();
        if (label != null) label.text = string.Empty;
    }
}
