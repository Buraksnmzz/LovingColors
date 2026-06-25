using System;
using UnityEngine;

namespace Gameplay.Levels
{
    public sealed class LevelService : ILevelService
    {
        private readonly LevelCatalog _levelCatalog;

        public LevelService(TextAsset levelConfig)
        {
            if (levelConfig == null)
            {
                throw new ArgumentNullException(nameof(levelConfig));
            }

            _levelCatalog = LevelCatalog.Parse(levelConfig);
        }

        public bool TryGetLevelById(int levelId, out LevelDefinition levelDefinition)
        {
            return _levelCatalog.TryGetLevel(levelId, out levelDefinition);
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
