using System;
using System.Collections.Generic;

namespace Api.Models
{
    [Serializable]
    public class ParentDashboard
    {
        public int totalChildren;
        public int activeChildren;
        public List<ChildSummary> childSummaries;
    }

    [Serializable]
    public class ChildSummary
    {
        public string childId;
        public string username;
        public int totalCoins;
        public int loginStreak;
        public string lastActivityAt;
    }

    [Serializable]
    public class ChildDetail
    {
        public string id;
        public string username;
        public int coins;
        public int loginStreak;
        public string lastActivityAt;
        public string avatarState;
        public string additionalData;
        public string lastLoginDate;
    }

    [Serializable]
    public class ChildActivity
    {
        public string id;
        public string childId;
        public int activityType;
        public string payload;
        public string createdAt;
    }
}