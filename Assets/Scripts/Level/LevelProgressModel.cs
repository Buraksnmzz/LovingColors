using SavedData;

namespace Level
{
    public class LevelProgressModel: IModel
    {
        public int CurrentLevelIndex { get; set; }
        public int CurrentLevelAttemptCount { get; set; }
    }
}