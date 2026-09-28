using System;

namespace Media
{
    public sealed class ImageProcessingException : Exception
    {
        #region Public Methods

        public ImageProcessingException(string message) : base(message)
        {
        }

        #endregion
    }
}
