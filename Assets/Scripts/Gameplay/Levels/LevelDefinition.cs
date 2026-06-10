using Home;
using Newtonsoft.Json;
using UnityEngine;

namespace Gameplay.Levels
{
    public sealed class LevelDefinition
    {
        [JsonProperty("levelId")]
        public int LevelId { get; private set; }

        [JsonProperty("rules")]
        public string RuleName { get; private set; }

        [JsonProperty("columnCount")]
        public int ColumnCount { get; private set; }

        [JsonProperty("rowCount")]
        public int RowCount { get; private set; }

        [JsonProperty("difficulty")]
        public string DifficultyRaw { get; private set; }

        [JsonProperty("cornerColors")]
        public CornerColorDefinition CornerColors { get; private set; }

        [JsonIgnore]
        public LevelDifficultyType Difficulty
        {
            get
            {
                if (System.Enum.TryParse(DifficultyRaw, true, out LevelDifficultyType difficulty))
                {
                    return difficulty;
                }

                return LevelDifficultyType.Normal;
            }
        }

        [JsonIgnore]
        public Color TopLeftColor => ParseColor(CornerColors?.TopLeft);

        [JsonIgnore]
        public Color TopRightColor => ParseColor(CornerColors?.TopRight);

        [JsonIgnore]
        public Color BottomLeftColor => ParseColor(CornerColors?.BottomLeft);

        [JsonIgnore]
        public Color BottomRightColor => ParseColor(CornerColors?.BottomRight);

        public bool IsValid()
        {
            return LevelId > 0
                   && RowCount > 0
                   && ColumnCount > 0
                   && !string.IsNullOrWhiteSpace(RuleName)
                   && CornerColors != null;
        }

        private static Color ParseColor(string htmlColor)
        {
            if (!string.IsNullOrWhiteSpace(htmlColor) && ColorUtility.TryParseHtmlString(htmlColor, out var color))
            {
                return color;
            }

            return Color.white;
        }
    }

    public sealed class CornerColorDefinition
    {
        [JsonProperty("topLeft")]
        public string TopLeft { get; private set; }

        [JsonProperty("topRight")]
        public string TopRight { get; private set; }

        [JsonProperty("bottomLeft")]
        public string BottomLeft { get; private set; }

        [JsonProperty("bottomRight")]
        public string BottomRight { get; private set; }
    }
}