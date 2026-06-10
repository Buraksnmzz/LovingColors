
using System;
using Gameplay.Levels;
using UnityEngine;

namespace Gameplay
{
    public class GameplayView : BaseView
    {
        public event Action Shown;

        [SerializeField] private TextAsset levelConfig;

        private Board _board;
        private LevelCatalog _levelCatalog;

        protected override void Awake()
        {
            base.Awake();
            _board = GetComponentInChildren<Board>(true);

            if (levelConfig != null)
            {
                _levelCatalog = LevelCatalog.Parse(levelConfig);
            }
            else
            {
                Debug.LogError("GameplayView level config is not assigned.");
            }
        }

        protected override void OnShown()
        {
            base.OnShown();
            Shown?.Invoke();
        }

        public bool TryGetLevelDefinition(int levelId, out LevelDefinition levelDefinition)
        {
            levelDefinition = null;
            return _levelCatalog != null && _levelCatalog.TryGetLevel(levelId, out levelDefinition);
        }

        public void InitializeBoard(LevelDefinition levelDefinition)
        {
            if (_board == null)
            {
                Debug.LogError("GameplayView could not find Board component in its children.");
                return;
            }

            _board.Initialize(levelDefinition);
        }
    }
}
