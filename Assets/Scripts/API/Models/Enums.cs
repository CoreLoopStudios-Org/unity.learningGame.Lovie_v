using System;

namespace Api.Models
{
    [Serializable]
    public enum UserType
    {
        Admin = 1,
        Parent = 2
    }

    [Serializable]
    public enum ActivityType
    {
        StoryRead = 1,
        QuizAttempt = 2,
        GamePlayed = 3,
        DailyLogin = 4
    }

    [Serializable]
    public enum ContentStatus
    {
        None = 0,
        Draft = 1,
        Published = 2
    }

    [Serializable]
    public enum PurchaseStatus
    {
        None = 0,
        Completed = 1
    }

    [Serializable]
    public enum AudioType
    {
        Narration = 1,
        BackgroundMusic = 2,
        SoundEffect = 3,
        FullStory = 4
    }
}