using System;
using System.Collections.Generic;

namespace Modules.GameFramework.Content
{
    /// <summary>
    /// Plain data representation of a single quiz question. Deserializable
    /// directly from JSON today; the same shape will later be built from
    /// a Firestore document with no change to any consuming script.
    /// </summary>
    [Serializable]
    public class QuestionData
    {
        public string questionText;
        public List<string> options;
        public int correctOptionIndex;
    }

    /// <summary>
    /// Plain data representation of a full Story Quest level: the story
    /// text plus its attached questions. Deserializable directly from
    /// JSON today; the same shape will later be built from a backend
    /// document with no change to any consuming script.
    /// </summary>
    [Serializable]
    public class StoryQuestLevel
    {
        public string storyId;
        public string title;
        public string storyType;
        public string content;
        public List<QuestionData> questions;
    }
}