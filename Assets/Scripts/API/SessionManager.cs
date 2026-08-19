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

                TokenPayload tokenData = JsonUtility.FromJson<TokenPayload>(decoded);
                return tokenData?.role;
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
                DateTime expiry = DateTime.Parse(expiresAt);
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
                OnSessionExpired?.Invoke();
            }
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

        private string Base64Decode(string input)
        {
            string padded = input.PadRight(input.Length + (4 - input.Length % 4) % 4, '=');
            System.Text.Encoding encoding = System.Text.Encoding.UTF8;

            byte[] data = System.Convert.FromBase64String(padded);
            return encoding.GetString(data);
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