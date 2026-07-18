using UnityEngine;

namespace Win
{
    [CreateAssetMenu(fileName = "BadgeSpriteConfig", menuName = "Loving Colors/Badge Sprite Config")]
    public class BadgeSpriteConfig : ScriptableObject
    {
        [SerializeField] private Sprite[] badgeSprites;

        public Sprite GetBadgeSprite(int badgeIndex)
        {
            if (badgeSprites == null || badgeSprites.Length == 0)
            {
                return null;
            }

            return badgeSprites[Mathf.Clamp(badgeIndex, 0, badgeSprites.Length - 1)];
        }
    }
}