using System;

namespace Api.Models
{
    [Serializable]
    public class ChildProfile
    {
        public string id;
        public string username;
        public int coins;
        public int loginStreak;
        public string avatarState;
        public string additionalData;
        public string lastLoginDate;
        public string lastActivityAt;
    }

    [Serializable]
    public class ChildStats
    {
        public int coins;
        public int loginStreak;
        public bool canClaimDailyReward;
        public string lastLoginDate;
        public string lastActivityAt;
    }

    [Serializable]
    public class DailyRewardResult
    {
        public bool alreadyClaimed;
        public int coinsAwarded;
        public int totalCoins;
        public int loginStreak;
    }

    [Serializable]
    public class ActivityLogged
    {
        public string id;
        public int activityType;
        public string createdAt;
        public int coinsEarned;
        public int totalCoins;
        public string message;
    }
}