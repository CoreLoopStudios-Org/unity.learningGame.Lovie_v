using UnityEngine;

namespace Modules.GameFramework.Content
{
    /// <summary>
    /// Loads Story Sequencing content from a JSON file in Resources.
    /// This is a temporary content source for the current milestone;
    /// a Firestore-backed implementation will replace it later behind
    /// the same <see cref="IStorySequencingContentRepository"/> interface.
    /// </summary>
    public class JsonStorySequencingContentRepository : IStorySequencingContentRepository
    {
        #region Fields

        private const string RESOURCES_FOLDER = "Stories";

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public StorySequencingEntry LoadStory(string storyId)
        {
            string resourcePath = $"{RESOURCES_FOLDER}/{storyId}";
            TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);

            if (jsonAsset == null)
            {
                Debug.LogError($"[JsonStorySequencingContentRepository] Could not find JSON at Resources/{resourcePath}.json");
                return null;
            }

            StorySequencingEntry entry = JsonUtility.FromJson<StorySequencingEntry>(jsonAsset.text);

            if (entry == null)
            {
                Debug.LogError($"[JsonStorySequencingContentRepository] Failed to parse JSON for story id '{storyId}'.");
            }

            return entry;
        }

        #endregion
    }
}
