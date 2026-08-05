using System;

namespace Modules.GameFramework.Content
{
    /// <summary>
    /// Plain data representation of a single sequencing event/sentence card
    /// belonging to a Story Sequencing story. Deserializable directly from
    /// JSON today; the same shape will later be built from a backend
    /// document with no change to any consuming script.
    /// </summary>
    [Serializable]
    public class StorySequencingEvent
    {
        public string Id;
        public string Text;
        public int CorrectPosition;
    }

    /// <summary>
    /// Plain data representation of a full Story Sequencing level: the
    /// story text plus the events the player must place in order.
    /// Deserializable directly from JSON today; the same shape will later
    /// be built from a backend document with no change to any consuming
    /// script.
    /// </summary>
    [Serializable]
    public class StorySequencingEntry
    {
        public string StoryId;
        public string Title;
        public string StoryText;
        public StorySequencingEvent[] Events;
    }
}
