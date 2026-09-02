using System;

namespace Api.Models
{
    [Serializable]
    public class Story
    {
        public string id;
        public string title;
        public string coverImageUrl;
        public string contentPayload;
        public int status;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    public class Quiz
    {
        public string id;
        public string title;
        public string storyId;
        public string questionsPayload;
        public int status;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    public class MiniGameContent
    {
        public string id;
        public string gameType;
        public string title;
        public int status;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    public class StoryAudio
    {
        public string id;
        public string storyId;
        public int? pageNumber;
        public string audioUrl;
        public string mimeType;
        public int type;
        public float? startTime;
        public float? endTime;
        public string language;
        public int? durationSeconds;
        public bool isActive;
        public string createdAt;
    }
}