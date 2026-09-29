using System.Threading.Tasks;
using Api.Endpoints;
using Media;

namespace Modules.Profile
{
    public sealed class AdminProfilePictureRepository : IProfilePictureRepository
    {
        private readonly AdminApi _adminApi;

        public AdminProfilePictureRepository(AdminApi adminApi)
        {
            _adminApi = adminApi;
        }

        public async Task<bool> UpdateProfilePictureAsync(byte[] imageBytes, string fileName)
        {
            string url = await _adminApi.UploadMediaAsync(imageBytes, fileName, MediaConstants.JPEG_MIME_TYPE);
            if (string.IsNullOrEmpty(url))
                return false;

            return await _adminApi.UpdateProfileAsync(profilePictureUrl: url);
        }
    }
}
