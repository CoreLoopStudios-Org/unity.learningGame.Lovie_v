using System.Threading.Tasks;

namespace Media
{
    public interface IImagePicker
    {
        bool IsBusy { get; }

        Task<ImagePickResult> PickImageAsync(string title);
    }
}
