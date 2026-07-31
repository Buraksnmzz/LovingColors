using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GetHint
{
    public class BoosterIntroView: BaseView
    {
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private TextMeshProUGUI infoText;
        [SerializeField] private Button getBoosterButton;
        [SerializeField] private Image boosterImage;
        [SerializeField] private Sprite pinSpriteMiddle;
        [SerializeField] private Sprite superPinSpriteMiddle;
        [SerializeField] private TextMeshProUGUI boosterRewardAmountText;
        
        public event Action GetBoosterButtonClicked;
        
        private Tween _topBarTween;
        private Tween _boosterFlyTween;
        private Vector3 _topBarInitialLocalPosition;
        private Vector3 _boosterImageInitialPosition;
        private GetHintPopupType _popupType = GetHintPopupType.Pin;
        
        private void Start()
        {
            getBoosterButton.onClick.AddListener(() => GetBoosterButtonClicked?.Invoke());
        }
        
        public void SetBoosterRewardAmountText(int amount)
        {
            boosterRewardAmountText.text = "x" + amount;
        }
        
        public void SetHeaderAndInfoText(string header, string info)
        {
            headerText.text = header;
            infoText.text = info;
        }
        
        public void ConfigurePopup(GetHintPopupType popupType)
        {
            _popupType = popupType;
            boosterImage.sprite = popupType == GetHintPopupType.Pin ? pinSpriteMiddle : superPinSpriteMiddle;
        }
    }
}