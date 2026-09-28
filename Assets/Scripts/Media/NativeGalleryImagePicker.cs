using System.Threading.Tasks;

namespace Media
{
    public sealed class NativeGalleryImagePicker : IImagePicker
    {
        #region Properties

        public bool IsBusy => NativeGallery.IsMediaPickerBusy();

        #endregion

        #region Public Methods

        public async Task<ImagePickResult> PickImageAsync(string title)
        {
            if (IsBusy)
                return ImagePickResult.WithoutFile(ImagePickStatus.Busy);

            NativeGallery.Permission permission = await NativeGallery.RequestPermissionAsync(
                NativeGallery.PermissionType.Read, NativeGallery.MediaType.Image);

            if (permission != NativeGallery.Permission.Granted)
                return ImagePickResult.WithoutFile(ImagePickStatus.PermissionDenied);

            string filePath = await OpenGalleryAsync(title);

            return string.IsNullOrEmpty(filePath)
                ? ImagePickResult.WithoutFile(ImagePickStatus.Cancelled)
                : ImagePickResult.Picked(filePath);
        }

        #endregion

        #region Private Methods

        private static Task<string> OpenGalleryAsync(string title)
        {
            var completion = new TaskCompletionSource<string>();
            NativeGallery.GetImageFromGallery(
                path => completion.TrySetResult(path),
                title,
                MediaConstants.IMAGE_PICK_MIME_FILTER);
            return completion.Task;
        }

        #endregion
    }
}
