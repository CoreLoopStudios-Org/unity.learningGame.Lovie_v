namespace Modules.GameFramework.Content
{
    /// <summary>
    /// Abstraction over where Story Sequencing level content comes from.
    /// Game code depends only on this interface, never on a concrete
    /// source, so the implementation can be swapped (JSON today,
    /// Firestore later) without touching any game logic.
    /// </summary>
    public interface IStorySequencingContentRepository
    {
        /// <summary>
        /// Loads a single Story Sequencing story by its story id.
        /// Returns null if the story could not be found or parsed.
        /// </summary>
        StorySequencingEntry LoadStory(string storyId);
    }
}
