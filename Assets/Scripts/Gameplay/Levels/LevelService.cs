using System;
using UnityEngine;

namespace Gameplay.Levels
{
    public sealed class LevelService : ILevelService
    {
        private readonly LevelCatalog _levelCatalog;
        private readonly LevelCatalog _dailyChallengeLevelCatalog;

        public LevelService(TextAsset levelConfig)
            : this(levelConfig, null)
        {
        }

        public LevelService(TextAsset levelConfig, TextAsset dailyChallengeLevelConfig)
        {
            if (levelConfig == null)
            {
                throw new ArgumentNullException(nameof(levelConfig));
            }

            _levelCatalog = LevelCatalog.Parse(levelConfig);
            _dailyChallengeLevelCatalog = dailyChallengeLevelConfig != null
                ? LevelCatalog.Parse(dailyChallengeLevelConfig)
                : _levelCatalog;
        }

        public bool TryGetLevelById(int levelId, out LevelDefinition levelDefinition)
        {
            return _levelCatalog.TryGetLevel(levelId, out levelDefinition);
        }

        public bool TryGetDailyChallengeLevelById(int levelId, out LevelDefinition levelDefinition)
        {
            return _dailyChallengeLevelCatalog.TryGetLevel(levelId, out levelDefinition);
        }

        public bool TryGetLevelByIndex(int levelIndex, out LevelDefinition levelDefinition)
        {
            var levelId = Mathf.Max(0, levelIndex) + 1;
            return TryGetLevelById(levelId, out levelDefinition);
        }

        public LevelDefinition GetCurrentOrPreviousLevelByIndex(int levelIndex)
        {
            var currentLevelIndex = Mathf.Max(0, levelIndex);
            while (currentLevelIndex >= 0)
            {
                if (TryGetLevelByIndex(currentLevelIndex, out var levelDefinition))
                {
                    return levelDefinition;
                }

                currentLevelIndex--;
            }

            return null;
        }
    }
}
