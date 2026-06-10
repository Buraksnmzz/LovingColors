using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using Button = UnityEngine.UI.Button;

namespace DefaultNamespace
{
    public class Card : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float SelectionIntroDuration = 0.18f;
        private const float SelectionAnimationStepDuration = 0.26f;
        private const float DragAnimationStepDuration = 0.18f;
        private static readonly Vector3 DefaultScale = Vector3.one;
        private static readonly Vector3 SelectionBaseScale = new(1.15f, 1.15f, 1f);
        private static readonly Vector3 SelectionSquashScale = new(1.03f, 0.97f, 1f);
        private static readonly Vector3 SelectionStretchScale = new(0.97f, 1.03f, 1f);
        private static readonly Vector3 DragSquashScale = new(1.04f, 0.96f, 1f);
        private static readonly Vector3 DragStretchScale = new(0.96f, 1.04f, 1f);
        private static readonly Vector3 TargetPreviewScale = new(0.9f, 0.9f, 1f);

        [SerializeField] private Button button;
        [SerializeField] private GameObject lockImage;
        [SerializeField] private GameObject selectImage;

        private bool _isLocked;
        private bool _isSelected;
        private bool _isDragging;
        private bool _isPointerDown;
        private bool _isTargetPreviewActive;
        private RectTransform _rectTransform;
        private Canvas _rootCanvas;
        private Tween _movementTween;
        private Tween _loopingScaleTween;
        private Tween _scaleTween;

        public event Action<Card> CardClicked;
        public event Action<Card> DragStarted;
        public event Action<Card> Dragged;
        public event Action<Card> DragEnded;

        private bool _isClickable;

        public int CardId { get; set; }
        public int Order { get; set; }

        public RectTransform RectTransform => _rectTransform;

        public bool IsClickable
        {
            get => _isClickable;
            set
            {
                _isClickable = value;
                RefreshInteractableState();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                // if (selectImage != null)
                // {
                //     selectImage.SetActive(value);
                // }

                if (_isDragging)
                {
                    return;
                }

                if (value)
                {
                    StartSelectionSquashStretch();
                    return;
                }

                RestoreIdleScale();
            }
        }

        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                _isLocked = value;
                RefreshInteractableState();
                if (lockImage != null)
                {
                    lockImage.SetActive(value);
                }
            }
        }

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _rootCanvas = GetComponentInParent<Canvas>();
            if (selectImage != null)
            {
                selectImage.SetActive(false);
            }

            RefreshInteractableState();
        }

        private void OnDisable()
        {
            _movementTween?.Kill();
            _loopingScaleTween?.Kill();
            _scaleTween?.Kill();
        }

        public void Initialize(Button targetButton, GameObject targetLockImage, GameObject targetSelectImage)
        {
            button = targetButton;
            lockImage = targetLockImage;
            selectImage = targetSelectImage;
            RefreshInteractableState();
        }

        public void MoveCard(Vector3 targetPosition, float duration)
        {
            TweenMoveTo((Vector2)targetPosition, duration);
        }

        public Tween TweenMoveTo(Vector2 targetPosition, float duration)
        {
            _movementTween?.Kill();
            _movementTween = _rectTransform.DOAnchorPos(targetPosition, duration)
                .SetEase(Ease.InOutQuad);
            return _movementTween;
        }

        public void SnapTo(Vector2 anchoredPosition)
        {
            _movementTween?.Kill();
            _rectTransform.anchoredPosition = anchoredPosition;
        }

        public void SetTargetPreview(bool isActive)
        {
            if (_isTargetPreviewActive == isActive)
            {
                return;
            }

            _isTargetPreviewActive = isActive;
            _scaleTween?.Kill();
            _scaleTween = _rectTransform.DOScale(isActive ? TargetPreviewScale : DefaultScale, 0.14f)
                .SetEase(Ease.OutQuad);
        }

        public void StartDragSquashStretch()
        {
            StartLoopingScaleAnimation(DragSquashScale, DragStretchScale, DragAnimationStepDuration, Vector3.one);
        }

        public void StopDragSquashStretch()
        {
            if (_isSelected)
            {
                StartSelectionSquashStretch();
                return;
            }

            RestoreIdleScale();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!CanInteract() || _isDragging)
            {
                return;
            }

            CardClicked?.Invoke(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanInteract())
            {
                return;
            }

            _isPointerDown = true;
            _isDragging = true;
            DragStarted?.Invoke(this);
            StartDragSquashStretch();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging)
            {
                return;
            }

            var scaleFactor = _rootCanvas != null ? _rootCanvas.scaleFactor : 1f;
            _rectTransform.anchoredPosition += eventData.delta / scaleFactor;
            Dragged?.Invoke(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging)
            {
                return;
            }

            _isPointerDown = false;
            _isDragging = false;
            DragEnded?.Invoke(this);
        }

        private bool CanInteract()
        {
            return _isClickable && !_isLocked;
        }

        private void RefreshInteractableState()
        {
            if (button != null)
            {
                button.interactable = _isClickable && !_isLocked;
            }
        }

        private void StartSelectionSquashStretch()
        {
            _loopingScaleTween?.Kill();
            _scaleTween?.Kill();
            _scaleTween = _rectTransform.DOScale(SelectionBaseScale, SelectionIntroDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    if (!_isSelected || _isDragging)
                    {
                        return;
                    }

                    StartLoopingScaleAnimation(SelectionSquashScale, SelectionStretchScale, SelectionAnimationStepDuration, SelectionBaseScale);
                });
        }

        private void RestoreIdleScale()
        {
            _loopingScaleTween?.Kill();
            _scaleTween?.Kill();
            _scaleTween = _rectTransform.DOScale(DefaultScale, 0.12f)
                .SetEase(Ease.OutQuad);
        }

        private void StartLoopingScaleAnimation(Vector3 squashScale, Vector3 stretchScale, float stepDuration, Vector3 baseScale)
        {
            _loopingScaleTween?.Kill();
            _scaleTween?.Kill();

            var xAmplitude = ((squashScale.x - DefaultScale.x) + (DefaultScale.x - stretchScale.x)) * 0.5f * baseScale.x;
            var yAmplitude = ((DefaultScale.y - squashScale.y) + (stretchScale.y - DefaultScale.y)) * 0.5f * baseScale.y;
            var cycleDuration = stepDuration * 4f;

            _rectTransform.localScale = baseScale;
            _loopingScaleTween = DOVirtual.Float(0f, Mathf.PI * 2f, cycleDuration, angle =>
                {
                    var wave = Mathf.Sin(angle);
                    _rectTransform.localScale = new Vector3(
                        baseScale.x + xAmplitude * wave,
                        baseScale.y - yAmplitude * wave,
                        baseScale.z);
                })
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);
        }

    }
}