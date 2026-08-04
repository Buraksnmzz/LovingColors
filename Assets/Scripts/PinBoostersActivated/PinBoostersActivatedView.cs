using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace PinBoostersActivated
{
    public class PinBoostersActivatedView : BaseView
    {
        [SerializeField] private Button okButton;
        [SerializeField] private Transform image1;
        [SerializeField] private Transform image2;
        [SerializeField] private float pulseMinScale = 0.95f;
        [SerializeField] private float pulseMaxScale = 1.05f;
        [SerializeField] private float pulseDuration = 0.8f;

        private Sequence _image1PulseSequence;
        private Sequence _image2PulseSequence;

        public event Action OkButtonClicked;

        private void Start()
        {
            okButton.onClick.AddListener(OnOkButtonClicked);
            StartPulseAnimation();
        }

        protected override void OnDestroy()
        {
            if (okButton != null)
            {
                okButton.onClick.RemoveListener(OnOkButtonClicked);
            }

            StopPulseAnimation();
            base.OnDestroy();
        }

        private void StartPulseAnimation()
        {
            if (image1 != null)
            {
                _image1PulseSequence = CreatePulseSequence(image1);
            }

            if (image2 != null)
            {
                _image2PulseSequence = CreatePulseSequence(image2);
            }
        }

        private Sequence CreatePulseSequence(Transform target)
        {
            if (target == null)
            {
                return null;
            }

            target.localScale = Vector3.one * pulseMaxScale;
            var sequence = DOTween.Sequence();
            sequence.Append(target.DOScale(Vector3.one * pulseMinScale, pulseDuration).SetEase(Ease.InOutSine));
            sequence.SetLoops(-1, LoopType.Yoyo);
            return sequence;
        }

        private void StopPulseAnimation()
        {
            _image1PulseSequence?.Kill(true);
            _image2PulseSequence?.Kill(true);
            _image1PulseSequence = null;
            _image2PulseSequence = null;
        }

        private void OnOkButtonClicked()
        {
            OkButtonClicked?.Invoke();
        }
    }
}