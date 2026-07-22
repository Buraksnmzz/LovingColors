using System;
using DG.Tweening;
using General;
using Sound;
using UnityEngine;
using UnityEngine.EventSystems;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

namespace DefaultNamespace
{
    public class Card : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float SelectionIntroDuration = 0.18f;
        private const float SelectionAnimationStepDuration = 0.26f;
        private const float DragAnimationStepDuration = 0.18f;
        private const float AlphaHitTestMinimumThreshold = 0.1f;
        private const float DefaultPixelsPerUnitMultiplier = 1f;
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
        
        private ISoundService _soundService;
        private IHapticService _hapticService;

        private bool _isLocked;
        private bool _isSelected;
        private bool _isDragging;
        private bool _isPointerDown;
        private bool _isTargetPreviewActive;
        private RectTransform _rectTransform;
        private Canvas _rootCanvas;
        private Image _cardImage;
        private Tween _movementTween;
        private Tween _loopingScaleTween;
        private Tween _scaleTween;
        private Tween _rotationTween;
        private Vector3 _baseScale = Vector3.one;
        private float _baseRotation;

        public event Action<Card> CardClicked;
        public event Action<Card> DragStarted;
        public event Action<Card> Dragged;
        public event Action<Card> DragEnded;

        private bool _isClickable;

        public int CardId { get; set; }
        public int Order { get; set; }
        public int PieceType { get; set; }

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
            _cardImage = GetComponent<Image>();
            _soundService = ServiceLocator.GetService<ISoundService>();
            _hapticService = ServiceLocator.GetService<IHapticService>();
            ConfigureCardHitTest();
            RefreshSlicedPixelsPerUnitMultiplier();
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
            ConfigureCardHitTest();
            RefreshSlicedPixelsPerUnitMultiplier();
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
            RefreshSlicedPixelsPerUnitMultiplier();
        }

        public void SnapRotation(float zAngle)
        {
            _rotationTween?.Kill();
            _baseRotation = zAngle;
            _rectTransform.localRotation = Quaternion.Euler(0f, 0f, zAngle);
        }

        public Tween TweenRotateTo(float zAngle, float duration)
        {
            _rotationTween?.Kill();
            _baseRotation = zAngle;
            _rotationTween = _rectTransform.DOLocalRotate(new Vector3(0f, 0f, zAngle), duration)
                .SetEase(Ease.InOutQuad);
            return _rotationTween;
        }

        public void SetBaseScale(Vector3 baseScale)
        {
            _baseScale = baseScale;
            _rectTransform.localScale = baseScale;
            RefreshSlicedPixelsPerUnitMultiplier();
        }

        public void RefreshSlicedPixelsPerUnitMultiplier()
        {
            if (_cardImage == null || _cardImage.type != Image.Type.Sliced)
            {
                return;
            }

            var rect = _rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                _cardImage.pixelsPerUnitMultiplier = DefaultPixelsPerUnitMultiplier;
                return;
            }

            _cardImage.pixelsPerUnitMultiplier = Mathf.Sqrt(rect.height / rect.width);
        }

        public void SetTargetPreview(bool isActive)
        {
            if (_isTargetPreviewActive == isActive)
            {
                return;
            }

            _isTargetPreviewActive = isActive;
            _scaleTween?.Kill();
            _scaleTween = _rectTransform.DOScale(isActive ? Vector3.Scale(_baseScale, TargetPreviewScale) : _baseScale, 0.14f)
                .SetEase(Ease.OutQuad);
        }

        public void StartDragSquashStretch()
        {
            StartLoopingScaleAnimation(DragSquashScale, DragStretchScale, DragAnimationStepDuration, _baseScale);
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
            _soundService.PlaySound(ClipName.CardSelect);
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
            _soundService.PlaySound(ClipName.CardSelect);
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

        private void ConfigureCardHitTest()
        {
            if (_cardImage == null)
            {
                return;
            }

            _cardImage.alphaHitTestMinimumThreshold = AlphaHitTestMinimumThreshold;
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
            _scaleTween = _rectTransform.DOScale(_baseScale, 0.12f)
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