using UnityEngine;

namespace Media
{
    public static class RawImageCropUtility
    {
        #region Fields

        private static readonly Rect FULL_UV_RECT = new Rect(0f, 0f, 1f, 1f);

        #endregion

        #region Public Methods

        public static Rect CalculateCoverUvRect(int textureWidth, int textureHeight, Vector2 targetSize)
        {
            if (textureWidth <= 0 || textureHeight <= 0 || targetSize.x <= 0f || targetSize.y <= 0f)
                return FULL_UV_RECT;

            float textureAspect = (float)textureWidth / textureHeight;
            float targetAspect = targetSize.x / targetSize.y;

            if (textureAspect > targetAspect)
            {
                float visibleWidth = targetAspect / textureAspect;
                return new Rect((1f - visibleWidth) * 0.5f, 0f, visibleWidth, 1f);
            }

            float visibleHeight = textureAspect / targetAspect;
            return new Rect(0f, (1f - visibleHeight) * 0.5f, 1f, visibleHeight);
        }

        #endregion
    }
}
