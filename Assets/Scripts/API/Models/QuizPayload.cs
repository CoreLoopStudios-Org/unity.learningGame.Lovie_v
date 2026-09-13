using System;

namespace Api.Models
{
    // Client-defined schema for Quiz.questionsPayload — the backend stores it as an opaque string.
    [Serializable]
    public class QuizPayload
    {
        public QuizPayloadQuestion[] questions;
    }

    [Serializable]
    public class QuizPayloadQuestion
    {
        public string question;
        public string[] options;
        public int correctIndex;
    }
}
