using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SteelFlameAbyss.Battle
{
    //마우스를 올린 카드를 전용 레이어로 옮겨 다른 카드보다 앞에 표시합니다.
    public sealed class CardHoverTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float riseDistance = 100f;
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float duration = 0.2f;

        [SerializeField] private Ease enterEase = Ease.OutCubic;
        [SerializeField] private Ease exitEase = Ease.OutCubic;

        [SerializeField] private RectTransform hoverLayer;

        private RectTransform cardRect;
        private RectTransform cardSlot;
        private Vector2 slotAnchorMin;
        private Vector2 slotAnchorMax;
        private Vector2 slotPivot;
        private Vector2 slotSizeDelta;
        private Vector3 slotAnchoredPosition;
        private Vector3 slotLocalPosition;
        private Quaternion slotLocalRotation;
        private Vector3 slotLocalScale;
        private Vector2 hoverRestingPosition;
        private Sequence currentTween;
        private bool isHovered;
        private bool isReturning;
        private bool isReady;

        //카드와 현재 슬롯을 저장하고 연결된 호버 레이어를 초기화합니다.
        private void Awake()
        {
            cardRect = transform as RectTransform;
            Initialize(transform.parent as RectTransform, hoverLayer);
        }

        //카드가 다시 활성화될 때 현재 슬롯 기준으로 RectTransform 정보를 갱신합니다.
        private void OnEnable()
        {
            if (!isHovered)
                Initialize(transform.parent as RectTransform, hoverLayer);
        }

        //카드가 사용할 슬롯과 호버 레이어를 전달받아 저장합니다.
        public void Initialize(RectTransform slot, RectTransform layer)
        {
            cardRect ??= transform as RectTransform;
            cardSlot = slot;
            hoverLayer = layer;
            isReady = cardRect != null && cardSlot != null && hoverLayer != null;

            if (isReady)
                CacheSlotLayout();
        }

        //마우스가 카드에 들어오면 전용 레이어로 옮긴 뒤 위로 이동하고 확대합니다.
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isReady)
            {
                Debug.LogWarning($"[카드 호버] {name}: CardSlot 또는 CardHoverLayer 연결이 필요합니다.", this);
                return;
            }

            if (isHovered)
            {
                if (isReturning)
                {
                    isReturning = false;
                    PlayHoverTween();
                }

                return;
            }

            CacheSlotLayout();
            isHovered = true;
            isReturning = false;

            var worldPosition = cardRect.position;
            var worldRotation = cardRect.rotation;
            var renderedSize = cardRect.rect.size;
            cardRect.SetParent(hoverLayer, true);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = slotPivot;
            cardRect.sizeDelta = renderedSize;
            cardRect.position = worldPosition;
            cardRect.rotation = worldRotation;
            cardRect.SetAsLastSibling();
            hoverRestingPosition = cardRect.anchoredPosition;
            PlayHoverTween();
        }

        //마우스가 카드에서 나가면 카드 슬롯 위치로 돌아갑니다.
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isReady || !isHovered || isReturning)
                return;

            isReturning = true;
            PlayReturnTween();
        }

        //슬롯 안에서 사용하던 RectTransform 값을 모두 저장합니다.
        private void CacheSlotLayout()
        {
            slotAnchorMin = cardRect.anchorMin;
            slotAnchorMax = cardRect.anchorMax;
            slotPivot = cardRect.pivot;
            slotSizeDelta = cardRect.sizeDelta;
            slotAnchoredPosition = cardRect.anchoredPosition3D;
            slotLocalPosition = cardRect.localPosition;
            slotLocalRotation = cardRect.localRotation;
            slotLocalScale = cardRect.localScale;
        }

        //카드를 현재 위치에서 위로 올리고 조금 확대합니다.
        private void PlayHoverTween()
        {
            currentTween?.Kill();
            currentTween = DOTween.Sequence()
                .Join(cardRect.DOAnchorPos(hoverRestingPosition + Vector2.up * riseDistance, duration))
                .Join(cardRect.DOScale(slotLocalScale * hoverScale, duration))
                .SetEase(enterEase)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        //카드를 원래 슬롯의 화면 위치로 움직인 뒤 슬롯 아래에 다시 넣습니다.
        private void PlayReturnTween()
        {
            currentTween?.Kill();
            var targetWorldPosition = cardSlot.TransformPoint(slotLocalPosition);
            currentTween = DOTween.Sequence()
                .Join(cardRect.DOMove(targetWorldPosition, duration))
                .Join(cardRect.DOScale(slotLocalScale, duration))
                .SetEase(exitEase)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(RestoreToSlot);
        }

        //카드를 원래 슬롯의 자식으로 되돌리고 로컬 Transform 값을 복구합니다.
        private void RestoreToSlot()
        {
            if (cardRect == null || cardSlot == null)
                return;

            cardRect.SetParent(cardSlot, false);
            cardRect.anchorMin = slotAnchorMin;
            cardRect.anchorMax = slotAnchorMax;
            cardRect.pivot = slotPivot;
            cardRect.sizeDelta = slotSizeDelta;
            cardRect.anchoredPosition3D = slotAnchoredPosition;
            cardRect.localRotation = slotLocalRotation;
            cardRect.localScale = slotLocalScale;
            isHovered = false;
            isReturning = false;
        }

        //비활성화될 때 애니메이션을 제거하고 카드를 슬롯으로 즉시 복구합니다.
        private void OnDisable()
        {
            currentTween?.Kill();
            currentTween = null;

            if (isHovered)
                RestoreToSlot();
        }
    }
}
