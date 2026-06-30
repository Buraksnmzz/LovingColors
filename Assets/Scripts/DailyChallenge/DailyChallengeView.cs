using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DailyChallenge
{
    public class DailyChallengeView : BaseView
    {
        public event Action PreviousMonthClicked;
        public event Action NextMonthClicked;
        public event Action PlayClicked;
        public event Action<int> DayClicked;
        public event Action DebugCompleteDayClicked;
        public event Action DebugResetAllDaysClicked;
        public event Action DebugCompleteAllDaysClicked;
        public event Action BackButtonClicked;

        [SerializeField] private Button previousMonthButton;
        [SerializeField] private Button nextMonthButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button debugCompleteDayButton;
        [SerializeField] private Button debugResetAllDaysButton;
        [SerializeField] private Button debugCompleteAllDaysButton;
        [SerializeField] private TextMeshProUGUI monthText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Transform dayContainer;
        [SerializeField] private DailyChallengeDayView dayPrefab;

        private readonly List<DailyChallengeDayView> _dayViews = new List<DailyChallengeDayView>();

        private void Start()
        {
            previousMonthButton.onClick.AddListener(() => PreviousMonthClicked?.Invoke());
            nextMonthButton.onClick.AddListener(() => NextMonthClicked?.Invoke());
            playButton.onClick.AddListener(() => PlayClicked?.Invoke());
            debugCompleteDayButton.onClick.AddListener(() => DebugCompleteDayClicked?.Invoke());
            debugResetAllDaysButton.onClick.AddListener(() => DebugResetAllDaysClicked?.Invoke());
            debugCompleteAllDaysButton.onClick.AddListener(() => DebugCompleteAllDaysClicked?.Invoke());
            backButton.onClick.AddListener(() => BackButtonClicked?.Invoke());
        }

        public void SetMonthText(string value)
        {
            monthText.text = value;
        }

        public void SetNavigationButtons(bool canGoPrevious, bool canGoNext)
        {
            previousMonthButton.interactable = canGoPrevious;
            nextMonthButton.interactable = canGoNext;
        }

        public void SetPlayButton(bool isInteractable)
        {
            playButton.gameObject.SetActive(isInteractable);
        }

        public void SetProgress(int completedCount, int activeCount)
        {
            progressText.text = completedCount + "/" + activeCount;
        }

        public void SetDays(IReadOnlyList<DailyChallengeDayModel> days)
        {
            ClearDays();

            if (dayPrefab == null || dayContainer == null)
                return;

            foreach (var day in days)
            {
                var dayView = Instantiate(dayPrefab, dayContainer);
                dayView.Clicked += OnDayClicked;
                dayView.SetDay(day);
                _dayViews.Add(dayView);
            }
        }

        protected override void OnDestroy()
        {
            if (previousMonthButton != null)
                previousMonthButton.onClick.RemoveAllListeners();
            if (nextMonthButton != null)
                nextMonthButton.onClick.RemoveAllListeners();
            if (playButton != null)
                playButton.onClick.RemoveAllListeners();
            if (debugCompleteDayButton != null)
                debugCompleteDayButton.onClick.RemoveAllListeners();
            if (debugResetAllDaysButton != null)
                debugResetAllDaysButton.onClick.RemoveAllListeners();
            if (debugCompleteAllDaysButton != null)
                debugCompleteAllDaysButton.onClick.RemoveAllListeners();

            ClearDays();

            base.OnDestroy();
        }

        private void ClearDays()
        {
            foreach (var dayView in _dayViews)
            {
                if (dayView == null)
                    continue;

                dayView.Clicked -= OnDayClicked;
                Destroy(dayView.gameObject);
            }

            _dayViews.Clear();
        }

        private void OnDayClicked(int day)
        {
            DayClicked?.Invoke(day);
        }
    }
}