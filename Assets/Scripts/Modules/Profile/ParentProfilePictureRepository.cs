using System.Threading.Tasks;
using Api.Endpoints;
using Media;

namespace Modules.Profile
{
    public sealed class ParentProfilePictureRepository : IProfilePictureRepository
    {
        private readonly ParentApi _parentApi;

        public ParentProfilePictureRepository(ParentApi parentApi)
        {
            _parentApi = parentApi;
        }

        public async Task<bool> UpdateProfilePictureAsync(byte[] imageBytes, string fileName)
        {
            string url = await _parentApi.UploadMediaAsync(imageBytes, fileName, MediaConstants.JPEG_MIME_TYPE);
            if (string.IsNullOrEmpty(url))
                return false;

            return await _parentApi.UpdateProfileAsync(profileImageUrl: url);
        }
    }
}
