using System.Threading.Tasks;

namespace Modules.Profile
{
    public interface IProfilePictureRepository
    {
        Task<bool> UpdateProfilePictureAsync(byte[] imageBytes, string fileName);
    }
}
