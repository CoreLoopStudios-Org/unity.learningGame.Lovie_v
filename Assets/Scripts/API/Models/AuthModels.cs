using System;

namespace Api.Models
{
    [Serializable]
    public class AuthResponse
    {
        public string token;
        public string tokenType;
        public string expiresAt;
    }

    [Serializable]
    public class ChildAuthResponse
    {
        public string token;
        public string tokenType;
        public string expiresAt;
        public string childId;
        public string username;
        public int coins;
        public int loginStreak;
    }
}