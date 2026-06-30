using General;

namespace Gameplay.Levels
{
    public interface ILevelService : IService
    {
        bool TryGetLevelById(int levelId, out LevelDefinition levelDefinition);
        bool TryGetDailyChallengeLevelById(int levelId, out LevelDefinition levelDefinition);
        bool TryGetLevelByIndex(int levelIndex, out LevelDefinition levelDefinition);
        LevelDefinition GetCurrentOrPreviousLevelByIndex(int levelIndex);
    }
}
