using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Media
{
    public sealed class ProcessedImage : IDisposable
    {
        #region Properties

        public Texture2D PreviewTexture { get; private set; }

        public byte[] JpegBytes { get; }

        public string FileName { get; }

        #endregion

        #region Public Methods

        public ProcessedImage(Texture2D previewTexture, byte[] jpegBytes, string fileName)
        {
            PreviewTexture = previewTexture;
            JpegBytes = jpegBytes;
            FileName = fileName;
        }

        public void Dispose()
        {
            if (PreviewTexture != null)
                Object.Destroy(PreviewTexture);

            PreviewTexture = null;
        }

        #endregion
    }
}
