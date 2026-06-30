using General;
using System;
using System.Collections.Generic;

namespace DailyChallenge
{
    public interface IDailyChallengeService : IService
    {
        int InstallYear { get; }
        int InstallMonth { get; }
        int CurrentYear { get; }
        int CurrentMonth { get; }
        DateTime CurrentMonthDate { get; }
        DateTime DisplayedMonthDate { get; }
        DateTime FirstAvailableMonthDate { get; }
        DateTime LastAvailableMonthDate { get; }
        bool HasActiveDailyChallengeGame { get; }

        IReadOnlyList<DailyChallengeDayModel> GetCalendarGrid();
        IReadOnlyList<DailyChallengeDay> GetDisplayedMonthDays();
        DailyChallengeDay GetDay(int day);
        DailyChallengeDay GetDay(DateTime date);
        DailyChallengeDay GetSelectedDay();
        DateTime? GetSelectedDate();
        DateTime? GetPlayedDate();
        string GetPlayedDateText();
        int GetSelectedLevelId();
        int GetPlayedLevelId();
        int GetLevelId(DateTime date);
        int GetCompletedCountInDisplayedMonth();
        int GetActiveCountInDisplayedMonth();
        int GetTotalCountInDisplayedMonth();
        bool CanNavigateToPreviousMonth();
        bool CanNavigateToNextMonth();
        void NavigateToPreviousMonth();
        void NavigateToNextMonth();
        void SetDisplayedMonth(int year, int month);
        bool SelectDay(int day);
        bool SelectDate(DateTime date);
        bool SelectLatestPlayableUncompletedDayFromCurrentDate();
        bool SelectLatestPlayableUncompletedDay();
        bool SelectFirstPlayableUncompletedDay();
        void StartSelectedDayGame();
        void ClearActiveDailyChallengeGame();
        void CompletePlayedDay();
        void CompleteSelectedDay();
        void ResetAllDays();
        void CompleteAllActiveDays();
    }
}