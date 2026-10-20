using Api.Models;
using UnityEngine;

namespace Api.Endpoints
{
    public class AuthApi
    {
        private readonly ApiClient client;

        public AuthApi(ApiClient apiClient)
        {
            client = apiClient;
        }

        public async Awaitable<AuthResponse> RegisterAsync(string email, string password, string fullName, UserType userType)
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

        public async Awaitable<AuthResponse> LoginAsync(string email, string password)
        {
            var data = new
            {
                email,
                password
            };

            return await client.PostAsync<AuthResponse>("/api/auth/login", data);
        }

        public async Awaitable<ChildAuthResponse> ChildLoginAsync(string username, string password, string parentId = null)
        {
            var data = new
            {
                username,
                password,
                parentId
            };

            return await client.PostAsync<ChildAuthResponse>("/api/auth/child/login", data);
        }

        /// <summary>Exchanges a native Google ID token (Credential Manager / iOS OAuth) for a session.</summary>
        public async Awaitable<AuthResponse> GoogleLoginAsync(string idToken)
        {
            var data = new GoogleLoginRequest
            {
                idToken = idToken
            };

            return await client.PostAsync<AuthResponse>("/api/auth/google", data);
        }

        /// <summary>
        /// Exchanges a native Sign in with Apple identity token for a session.
        /// First/last name are only reported by Apple on the very first sign-in
        /// and may be null afterwards.
        /// </summary>
        public async Awaitable<AuthResponse> AppleLoginAsync(string idToken, string firstName = null, string lastName = null)
        {
            var data = new AppleLoginRequest
            {
                idToken = idToken,
                firstName = firstName,
                lastName = lastName
            };

            return await client.PostAsync<AuthResponse>("/api/auth/apple", data);
        }

        public async Awaitable<object> SendVerificationAsync(string email)
        {
            var data = new { email };
            return await client.PostAsync<object>("/api/auth/send-verification", data);
        }

        public async Awaitable<object> VerifyEmailAsync(string email, string otp)
        {
            var data = new
            {
                email,
                otp
            };

            return await client.PostAsync<object>("/api/auth/verify-email", data);
        }

        public async Awaitable<object> SendResetOtpAsync(string email)
        {
            var data = new { email };
            return await client.PostAsync<object>("/api/auth/send-reset-otp", data);
        }

        public async Awaitable<object> ResetPasswordAsync(string email, string otp, string newPassword)
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