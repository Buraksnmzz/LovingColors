using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Gameplay.Levels
{
    public sealed class LevelCatalog
    {
        private readonly Dictionary<int, LevelDefinition> _levelsById;

        private LevelCatalog(IEnumerable<LevelDefinition> levels)
        {
            _levelsById = levels.ToDictionary(level => level.LevelId);
        }

        public static LevelCatalog Parse(TextAsset asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            var levels = JsonConvert.DeserializeObject<List<LevelDefinition>>(asset.text);
            if (levels == null)
            {
                throw new InvalidOperationException("Level json could not be parsed.");
            }

            var validLevels = levels.Where(level => level != null && level.IsValid()).ToList();
            if (validLevels.Count == 0)
            {
                throw new InvalidOperationException("Level json does not contain any valid level definitions.");
            }

            return new LevelCatalog(validLevels);
        }

        public bool TryGetLevel(int levelId, out LevelDefinition levelDefinition)
        {
            return _levelsById.TryGetValue(levelId, out levelDefinition);
        }
    }
}