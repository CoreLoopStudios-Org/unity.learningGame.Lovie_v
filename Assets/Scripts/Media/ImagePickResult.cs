namespace Media
{
    public readonly struct ImagePickResult
    {
        #region Properties

        public ImagePickStatus Status { get; }

        public string FilePath { get; }

        public bool IsPicked => Status == ImagePickStatus.Picked;

        #endregion

        #region Public Methods

        public static ImagePickResult Picked(string filePath)
        {
            return new ImagePickResult(ImagePickStatus.Picked, filePath);
        }

        public static ImagePickResult WithoutFile(ImagePickStatus status)
        {
            return new ImagePickResult(status, null);
        }

        #endregion

        #region Private Methods

        private ImagePickResult(ImagePickStatus status, string filePath)
        {
            Status = status;
            FilePath = filePath;
        }

        #endregion
    }
}
