using UnityEngine;

namespace Modules.GameFramework.Content
{
    /// <summary>
    /// Abstraction over where Story Quest level content comes from.
    /// Game code depends only on this interface, never on a concrete
    /// source, so the implementation can be swapped (JSON today,
    /// Firestore later) without touching any game logic.
    /// </summary>
    public interface IStoryQuestContentRepository
    {
        /// <summary>
        /// Loads a single Story Quest level by its story id.
        /// Returns null if the story could not be found or parsed.
        /// </summary>
        StoryQuestLevel LoadLevel(string storyId);
    }

    /// <summary>
    /// Loads Story Quest level content from a JSON file in Resources.
    /// This is a temporary content source for the current milestone;
    /// a Firestore-backed implementation will replace it later behind
    /// the same <see cref="IStoryQuestContentRepository"/> interface.
    /// </summary>
    public class JsonStoryQuestContentRepository : IStoryQuestContentRepository
    {
        #region Fields

        private const string RESOURCES_FOLDER = "Stories";

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public StoryQuestLevel LoadLevel(string storyId)
        {
            string resourcePath = $"{RESOURCES_FOLDER}/{storyId}";
            TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);

            if (jsonAsset == null)
            {
                Debug.LogError($"[JsonStoryQuestContentRepository] Could not find JSON at Resources/{resourcePath}.json");
                return null;
            }

            StoryQuestLevel level = JsonUtility.FromJson<StoryQuestLevel>(jsonAsset.text);

            if (level == null)
            {
                Debug.LogError($"[JsonStoryQuestContentRepository] Failed to parse JSON for story id '{storyId}'.");
            }

            return level;
        }

        #endregion
    }
}