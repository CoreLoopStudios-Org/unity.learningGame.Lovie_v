namespace Avatar
{
    /// <summary>
    /// Categories for avatar customization parts
    /// </summary>
    public enum AvatarPartCategory
    {
        None = 0,
        BodyColor = 1,
        Hair = 2,
        Dress = 3,
        Shoes = 4,
        Accessories = 5,
        Cosmetics = 6
    }

    /// <summary>
    /// Extensions for AvatarPartCategory
    /// </summary>
    public static class AvatarPartCategoryExtensions
    {
        /// <summary>
        /// Get the display name for a category
        /// </summary>
        public static string GetDisplayName(this AvatarPartCategory category)
        {
            return category switch
            {
                AvatarPartCategory.BodyColor => "Body Color",
                AvatarPartCategory.Hair => "Hair",
                AvatarPartCategory.Dress => "Dress",
                AvatarPartCategory.Shoes => "Shoes",
                AvatarPartCategory.Accessories => "Accessories",
                AvatarPartCategory.Cosmetics => "Cosmetics",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get the save key for a category
        /// </summary>
        public static string GetSaveKey(this AvatarPartCategory category)
        {
            return $"Avatar_{category}";
        }
    }
}
