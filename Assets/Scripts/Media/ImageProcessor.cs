using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace Media
{
    public sealed class ImageProcessor
    {
        #region Fields

        private readonly ImageUploadConfigSO _config;

        #endregion

        #region Public Methods

        public ImageProcessor(ImageUploadConfigSO config)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
        }

        public async Task<ProcessedImage> ProcessAsync(string filePath)
        {
            Texture2D source = await LoadTextureAsync(filePath, CalculateDecodeLimit(filePath));
            Texture2D output = null;

            try
            {
                output = CropAndResizeOnGpu(source);
                byte[] jpegBytes = await EncodeJpegAsync(output);
                ValidateSize(jpegBytes);

                output.Apply(false, true);
                return new ProcessedImage(output, jpegBytes, CreateFileName());
            }
            catch
            {
                if (output != null)
                    Object.Destroy(output);
                throw;
            }
            finally
            {
                Object.Destroy(source);
            }
        }

        #endregion

        #region Private Methods

        private int CalculateDecodeLimit(string filePath)
        {
            NativeGallery.ImageProperties properties;

            try
            {
                properties = NativeGallery.GetImageProperties(filePath);
            }
            catch (Exception)
            {
                return _config.FallbackDecodeLimit;
            }

            if (properties.width <= 0 || properties.height <= 0)
                return _config.FallbackDecodeLimit;

            float coverScale = Mathf.Max(
                CalculateCoverScale(properties.width, properties.height),
                CalculateCoverScale(properties.height, properties.width));

            int longestSide = Mathf.Max(properties.width, properties.height);
            return Mathf.CeilToInt(longestSide * Mathf.Min(1f, coverScale));
        }

        private float CalculateCoverScale(int width, int height)
        {
            return Mathf.Max((float)_config.OutputWidth / width, (float)_config.OutputHeight / height);
        }

        private static async Task<Texture2D> LoadTextureAsync(string filePath, int decodeLimit)
        {
            Texture2D texture = null;

            try
            {
                texture = await NativeGallery.LoadImageAtPathAsync(filePath, decodeLimit, false);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[ImageProcessor] Failed to load '{filePath}': {exception.Message}");
            }

            if (texture == null)
                throw new ImageProcessingException("This file couldn't be opened as an image. Please choose another photo.");

            return texture;
        }

        private Texture2D CropAndResizeOnGpu(Texture2D source)
        {
            Rect cropUv = RawImageCropUtility.CalculateCoverUvRect(
                source.width, source.height, new Vector2(_config.OutputWidth, _config.OutputHeight));

            float cropWidth = source.width * cropUv.width;
            float scale = Mathf.Min(1f, _config.OutputWidth / cropWidth);
            int width = Mathf.Max(1, Mathf.RoundToInt(cropWidth * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(width / _config.OutputAspect));

            RenderTexture renderTexture = RenderTexture.GetTemporary(
                width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previousActive = RenderTexture.active;

            Graphics.Blit(source, renderTexture, cropUv.size, cropUv.position);
            RenderTexture.active = renderTexture;

            var output = new Texture2D(width, height, TextureFormat.RGB24, false);
            output.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            output.Apply(false, false);

            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(renderTexture);

            return output;
        }

        private Task<byte[]> EncodeJpegAsync(Texture2D texture)
        {
            byte[] rawPixels = texture.GetRawTextureData();
            GraphicsFormat format = texture.graphicsFormat;
            uint width = (uint)texture.width;
            uint height = (uint)texture.height;
            int quality = _config.JpegQuality;

            return Task.Run(() => ImageConversion.EncodeArrayToJPG(rawPixels, format, width, height, 0, quality));
        }

        private void ValidateSize(byte[] jpegBytes)
        {
            if (jpegBytes == null || jpegBytes.Length == 0)
                throw new ImageProcessingException("The image couldn't be compressed. Please choose another photo.");

            if (jpegBytes.Length > _config.MaxUploadBytes)
                throw new ImageProcessingException($"The image is too large. The maximum is {_config.MaxUploadSizeMegabytes} MB.");
        }

        private string CreateFileName()
        {
            return $"{_config.UploadFileNamePrefix}_{Guid.NewGuid():N}{MediaConstants.JPEG_FILE_EXTENSION}";
        }

        #endregion
    }
}
