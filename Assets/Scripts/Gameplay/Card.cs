using System;
using DG.Tweening;
using General;
using General.EventDispatcher;
using UnityEngine;
using Button = UnityEngine.UI.Button;

namespace DefaultNamespace
{
    public class Card:MonoBehaviour
    {
        
        [SerializeField] private Button button;
        [SerializeField] private GameObject lockImage;
        [SerializeField] private GameObject selectImage;
        private bool _isLocked;
        private bool _isSelected;
        public event Action<Card> CardClicked;
        private bool _isClickable;
        private IEventDispatcherService _eventDispatcher;
        private int _activeCardMovements = 0;
        public int CardId { get; set; }
        public int Order { get; set; }
        public bool IsClickable
        {
            get => _isClickable;
            set
            {
                _isClickable = value;
                button.interactable = value;
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                selectImage.SetActive(value);
            }
        }
        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                _isLocked = value;
                button.interactable = !value;
                lockImage.SetActive(value);
            }
        }
        
        private void Start()
        {
            button.onClick.AddListener(HandleCardClick);
            _eventDispatcher = ServiceLocator.GetService<IEventDispatcherService>();
        }

        private void HandleCardClick()
        {
            CardClicked?.Invoke(this);
        }

        public void MoveCard(Vector3 targetPosition, float duration)
        {
            // _eventDispatcher.Dispatch(new CardSwapMovementSignal(false));
            // transform.DOMove(targetPosition, duration).SetEase(Ease.OutQuad).OnComplete(()=>
            // {
            //     _eventDispatcher.Dispatch(new CardSwapMovementSignal(true));
            // });
        }
 
    }
}