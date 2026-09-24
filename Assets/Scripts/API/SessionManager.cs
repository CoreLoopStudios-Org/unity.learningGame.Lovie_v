using UnityEngine;
using System;

namespace Api
{
    public class SessionManager : MonoBehaviour
    {
        private const string RoleClaimUri = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
        private const string NameIdClaimUri = "http://schemas.microsoft.com/ws/2008/06/identity/claims/nameid";

        private static SessionManager instance;
        public static SessionManager Instance => instance;

        private string token;
        private string expiresAt;
        private string role;
        private string childId;
        private string userId;
        private string email;

        public event Action OnSessionExpired;
        public event Action<string> OnTokenUpdated;

        public string Token => token;
        public string Role => role;
        public string ChildId => childId;
        public string UserId => userId;
        public string Email => email;
        public bool IsAuthenticated => !string.IsNullOrEmpty(token) && IsValidToken();
        public bool IsChildSession => role == "Child";

        public static string ExtractRoleFromTokenStatic(string jwtToken)
        {
            if (string.IsNullOrEmpty(jwtToken))
                return null;

            try
            {
                string[] parts = jwtToken.Split('.');
                if (parts.Length < 2)
                    return null;

                string payload = parts[1];
                string decoded = Base64Decode(payload);

                return ExtractRoleWithUri(decoded);
            }
            catch
            {
                return null;
            }
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSession();
        }

        private void LoadSession()
        {
            token = TokenStore.GetToken();
            expiresAt = TokenStore.GetExpiresAt();
            role = TokenStore.GetRole();
            childId = TokenStore.GetChildId();
            userId = ExtractUserIdFromToken(token);
            email = ExtractEmailFromToken(token);
        }

        public void SetSession(string newToken, string newExpiresAt, string newRole = null, string newChildId = null)
        {
            token = newToken;
            expiresAt = newExpiresAt;
            role = newRole ?? ExtractRoleFromTokenStatic(newToken);
            childId = newChildId ?? ExtractChildIdFromToken(newToken);
            userId = ExtractUserIdFromToken(newToken);
            email = ExtractEmailFromToken(newToken);

            TokenStore.SaveToken(token, expiresAt, role, childId);
            OnTokenUpdated?.Invoke(token);
        }

        public void ClearSession()
        {
            token = null;
            expiresAt = null;
            role = null;
            childId = null;
            userId = null;
            email = null;

            TokenStore.ClearToken();
        }

        public bool IsValidToken()
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(expiresAt))
                return false;

            try
            {
                // Parse as UTC, adjust to universal time to avoid device timezone offset bugs
                DateTime expiry = DateTime.Parse(expiresAt, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
                return DateTime.UtcNow < expiry;
            }
            catch
            {
                return false;
            }
        }

        public void CheckSessionExpiry()
        {
            if (IsAuthenticated && !IsValidToken())
            {
                HandleSessionExpired();
            }
        }

        public void HandleSessionExpired()
        {
            string previousRole = role;
            ClearSession();
            OnSessionExpired?.Invoke();
            RedirectToLogin(previousRole);
        }

        public void RedirectToLogin(string userRole = null)
        {
            string targetRole = userRole ?? role ?? "Child";
            string sceneName = targetRole switch
            {
                "Admin" => "Main Game/Admin/Admin Login",
                "Parent" => "Main Game/Parent/Parent Login",
                _ => "Main Game/Children/Login"
            };

            Debug.Log($"[SessionManager] Session expired or invalid. Redirecting to {sceneName}");
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        private string ExtractChildIdFromToken(string jwtToken)
        {
            try
            {
                TokenPayload tokenData = DecodeTokenPayload(jwtToken);

                if (tokenData?.role == "Child")
                    return tokenData?.nameid ?? tokenData?.sub;

                return null;
            }
            catch
            {
                return null;
            }
        }

        private string ExtractUserIdFromToken(string jwtToken)
        {
            try
            {
                TokenPayload tokenData = DecodeTokenPayload(jwtToken);

                return tokenData?.nameid
                    ?? tokenData?.sub
                    ?? ExtractUriClaimValue(DecodePayloadString(jwtToken), NameIdClaimUri);
            }
            catch
            {
                return null;
            }
        }

        private string ExtractEmailFromToken(string jwtToken)
        {
            try
            {
                TokenPayload tokenData = DecodeTokenPayload(jwtToken);
                return tokenData?.email;
            }
            catch
            {
                return null;
            }
        }

        private static TokenPayload DecodeTokenPayload(string jwtToken)
        {
            return JsonUtility.FromJson<TokenPayload>(DecodePayloadString(jwtToken));
        }

        private static string DecodePayloadString(string jwtToken)
        {
            if (string.IsNullOrEmpty(jwtToken))
                return null;

            string[] parts = jwtToken.Split('.');
            if (parts.Length < 2)
                return null;

            return Base64Decode(parts[1]);
        }

        private static string Base64Decode(string input)
        {
            // Convert base64url to base64
            string base64 = input.Replace('-', '+').Replace('_', '/');
            string padded = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            System.Text.Encoding encoding = System.Text.Encoding.UTF8;

            byte[] data = System.Convert.FromBase64String(padded);
            return encoding.GetString(data);
        }

        private static string ExtractRoleWithUri(string decodedPayload)
        {
            // JWT uses full URI for role claim: http://schemas.microsoft.com/ws/2008/06/identity/claims/role
            // Search JSON for the claim value
            try
            {
                // First try standard "role" key
                var tokenData = JsonUtility.FromJson<TokenPayload>(decodedPayload);
                if (!string.IsNullOrEmpty(tokenData?.role))
                    return tokenData.role;

                return ExtractUriClaimValue(decodedPayload, RoleClaimUri);
            }
            catch
            {
                return null;
            }
        }

        private static string ExtractUriClaimValue(string decodedPayload, string claimUri)
        {
            // Search for a full-URI claim key in the JSON string
            string key = "\"" + claimUri + "\"";
            int keyIndex = decodedPayload.IndexOf(key);
            if (keyIndex < 0)
                return null;

            int valueStart = keyIndex + key.Length + 1; // Skip colon
            // Skip whitespace
            while (valueStart < decodedPayload.Length && char.IsWhiteSpace(decodedPayload[valueStart]))
                valueStart++;

            if (valueStart < decodedPayload.Length && decodedPayload[valueStart] == '"')
            {
                int valueEnd = decodedPayload.IndexOf('"', valueStart + 1);
                if (valueEnd > valueStart)
                    return decodedPayload.Substring(valueStart + 1, valueEnd - valueStart - 1);
            }
            return null;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                CheckSessionExpiry();
            }
        }

        [Serializable]
        private class TokenPayload
        {
            public string role;
            public string nameid;
            public string sub;
            public string exp;
            public string email;
        }
    }
}