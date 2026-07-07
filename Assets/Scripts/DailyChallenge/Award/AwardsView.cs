using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DailyChallenge.Award
{
    public class AwardsView : BaseView
    {
        [SerializeField] private AwardMonth awardMonthPrefab;
        [SerializeField] private AwardYearSeparator awardYearSeparatorPrefab;
        [SerializeField] private GridLayoutGroup awardMonthGridPrefab;
        [SerializeField] private Transform awardContent;
        [SerializeField] private AwardMonthSpriteConfig awardMonthSpriteConfig;
        [SerializeField] private Button backButton;

        private readonly List<GameObject> _awardItems = new List<GameObject>();

        private void Start()
        {
            backButton.onClick.AddListener(Hide);
        }

        public void SetAwards(IReadOnlyList<AwardMonthModel> awards)
        {
            ClearAwards();

            if (awardContent == null || awardMonthPrefab == null || awards == null)
                return;

            var previousYear = 0;
            GridLayoutGroup currentMonthGrid = null;
            foreach (var award in awards)
            {
                if (award.Year != previousYear)
                {
                    if (previousYear != 0)
                        CreateYearSeparator(award.Year);

                    currentMonthGrid = CreateMonthGrid();
                    previousYear = award.Year;
                }

                if (currentMonthGrid == null)
                    continue;

                var awardMonth = Instantiate(awardMonthPrefab, currentMonthGrid.transform);
                awardMonth.SetMonth(award, awardMonthSpriteConfig);
            }
        }

        protected override void OnDestroy()
        {
            ClearAwards();
            base.OnDestroy();
        }

        private void CreateYearSeparator(int year)
        {
            if (awardYearSeparatorPrefab == null)
                return;

            var separator = Instantiate(awardYearSeparatorPrefab, awardContent);
            separator.SetYear(year);
            _awardItems.Add(separator.gameObject);
        }

        private void ClearAwards()
        {
            foreach (var item in _awardItems)
            {
                if (item != null)
                    Destroy(item);
            }

            _awardItems.Clear();
        }

        private GridLayoutGroup CreateMonthGrid()
        {
            if (awardMonthGridPrefab == null)
                return null;

            var monthGrid = Instantiate(awardMonthGridPrefab, awardContent);
            _awardItems.Add(monthGrid.gameObject);
            return monthGrid;
        }
    }
}