using UnityEngine;
using System;

namespace Api
{
    public class SessionManager : MonoBehaviour
    {
        private static SessionManager instance;
        public static SessionManager Instance => instance;

        private string token;
        private string expiresAt;
        private string role;
        private string childId;

        public event Action OnSessionExpired;
        public event Action<string> OnTokenUpdated;

        public string Token => token;
        public string Role => role;
        public string ChildId => childId;
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
        }

        public void SetSession(string newToken, string newExpiresAt, string newRole = null, string newChildId = null)
        {
            token = newToken;
            expiresAt = newExpiresAt;
            role = newRole ?? ExtractRoleFromTokenStatic(newToken);
            childId = newChildId ?? ExtractChildIdFromToken(newToken);

            TokenStore.SaveToken(token, expiresAt, role, childId);
            OnTokenUpdated?.Invoke(token);
        }

        public void ClearSession()
        {
            token = null;
            expiresAt = null;
            role = null;
            childId = null;

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
            if (string.IsNullOrEmpty(jwtToken))
                return null;

            try
            {
                string[] parts = jwtToken.Split('.');
                if (parts.Length < 2)
                    return null;

                string payload = parts[1];
                string decoded = Base64Decode(payload);

                TokenPayload tokenData = JsonUtility.FromJson<TokenPayload>(decoded);

                if (tokenData?.role == "Child")
                    return tokenData?.nameid ?? tokenData?.sub;

                return null;
            }
            catch
            {
                return null;
            }
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

                // Search for full URI claim in JSON string
                string roleKey = "\"http://schemas.microsoft.com/ws/2008/06/identity/claims/role\"";
                int keyIndex = decodedPayload.IndexOf(roleKey);
                if (keyIndex > 0)
                {
                    int valueStart = keyIndex + roleKey.Length + 1; // Skip colon
                    // Skip whitespace
                    while (valueStart < decodedPayload.Length && char.IsWhiteSpace(decodedPayload[valueStart]))
                        valueStart++;

                    if (valueStart < decodedPayload.Length && decodedPayload[valueStart] == '"')
                    {
                        int valueEnd = decodedPayload.IndexOf('"', valueStart + 1);
                        if (valueEnd > valueStart)
                            return decodedPayload.Substring(valueStart + 1, valueEnd - valueStart - 1);
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
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
        }
    }
}