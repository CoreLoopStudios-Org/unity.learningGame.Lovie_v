using Api.Models;
using System.Threading.Tasks;

namespace Api.Endpoints
{
    public class AuthApi
    {
        private readonly ApiClient client;

        public AuthApi(ApiClient apiClient)
        {
            client = apiClient;
        }

        public async Task Awaitable<AuthResponse> RegisterAsync(string email, string password, string fullName, UserType userType)
        {
            var data = new
            {
                email,
                password,
                fullName,
                userType = (int)userType
            };

            return await client.PostAsync<AuthResponse>("/api/auth/register", data);
        }

        public async Task Awaitable<AuthResponse> LoginAsync(string email, string password)
        {
            var data = new
            {
                email,
                password
            };

            return await client.PostAsync<AuthResponse>("/api/auth/login", data);
        }

        public async Task Awaitable<ChildAuthResponse> ChildLoginAsync(string username, string password, string parentId = null)
        {
            var data = new
            {
                username,
                password,
                parentId
            };

            return await client.PostAsync<ChildAuthResponse>("/api/auth/child/login", data);
        }

        public async Task Awaitable<object> SendVerificationAsync(string email)
        {
            var data = new { email };
            return await client.PostAsync<object>("/api/auth/send-verification", data);
        }

        public async Task Awaitable<object> VerifyEmailAsync(string email, string otp)
        {
            var data = new
            {
                email,
                otp
            };

            return await client.PostAsync<object>("/api/auth/verify-email", data);
        }

        public async Task Awaitable<object> SendResetOtpAsync(string email)
        {
            var data = new { email };
            return await client.PostAsync<object>("/api/auth/send-reset-otp", data);
        }

        public async Task Awaitable<object> ResetPasswordAsync(string email, string otp, string newPassword)
        {
            var data = new
            {
                email,
                otp,
                newPassword
            };

            return await client.PostAsync<object>("/api/auth/reset-password", data);
        }
    }
}