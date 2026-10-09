using UnityEngine;

namespace Api
{
    public static class TokenStore
    {
        private const string TokenKey = "auth_token";
        private const string ExpiresAtKey = "token_expires_at";
        private const string RoleKey = "auth_role";
        private const string ChildIdKey = "auth_child_id";

        private static readonly string ObfuscationKey = "UnityImagineMe2024";

        public static void SaveToken(string token, string expiresAt, string role = null, string childId = null)
        {
            // XOR alone yields control chars (incl. NUL) that PlayerPrefs truncates
            // (registry REG_SZ / Android XML) — Base64 keeps the stored string safe.
            PlayerPrefs.SetString(TokenKey, System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Obfuscate(token))));
            PlayerPrefs.SetString(ExpiresAtKey, expiresAt);

            if (!string.IsNullOrEmpty(role))
                PlayerPrefs.SetString(RoleKey, role);

            if (!string.IsNullOrEmpty(childId))
                PlayerPrefs.SetString(ChildIdKey, childId);

            PlayerPrefs.Save();
        }

        public static string GetToken()
        {
            if (!PlayerPrefs.HasKey(TokenKey))
                return null;

            try
            {
                string stored = PlayerPrefs.GetString(TokenKey);
                return Obfuscate(System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(stored)));
            }
            catch (System.FormatException)
            {
                // Pre-Base64 stored value (corrupted by the old XOR-only format)
                return null;
            }
        }

        public static string GetExpiresAt()
        {
            return PlayerPrefs.HasKey(ExpiresAtKey) ? PlayerPrefs.GetString(ExpiresAtKey) : null;
        }

        public static string GetRole()
        {
            return PlayerPrefs.HasKey(RoleKey) ? PlayerPrefs.GetString(RoleKey) : null;
        }

        public static string GetChildId()
        {
            return PlayerPrefs.HasKey(ChildIdKey) ? PlayerPrefs.GetString(ChildIdKey) : null;
        }

        public static void ClearToken()
        {
            PlayerPrefs.DeleteKey(TokenKey);
            PlayerPrefs.DeleteKey(ExpiresAtKey);
            PlayerPrefs.DeleteKey(RoleKey);
            PlayerPrefs.DeleteKey(ChildIdKey);
            PlayerPrefs.Save();
        }

        public static bool IsTokenValid()
        {
            string token = GetToken();
            string expiresAt = GetExpiresAt();

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(expiresAt))
                return false;

            try
            {
                System.DateTime expiry = System.DateTime.Parse(expiresAt);
                return System.DateTime.UtcNow < expiry;
            }
            catch
            {
                return false;
            }
        }

        private static string Obfuscate(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            char[] result = new char[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                result[i] = (char)(input[i] ^ ObfuscationKey[i % ObfuscationKey.Length]);
            }
            return new string(result);
        }
    }
}