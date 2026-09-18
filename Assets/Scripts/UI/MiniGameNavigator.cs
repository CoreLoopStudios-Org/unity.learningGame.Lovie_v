using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class MiniGameNavigator : MonoBehaviour
    {
        public const string MainMenuScene = "Main Menu";
        public const string StoryQuestScene = "Scenes/Mini Games/Story Quest Mini Game";
        public const string ReadingDetectiveScene = "Scenes/Mini Games/Reading Detective Mini Game";
        public const string StorySequencingScene = "Scenes/Mini Games/Story Sequencing Mini Game";
        public const string WordWizardScene = "Scenes/Mini Games/Word Wizard mini Game";
        public const string PrefixSuffixScene = "Scenes/Mini Games/Prefix Suffix Mini";
        public const string RhymeTimeScene = "Scenes/Mini Games/Rhyme Time Mini Game";
        public const string WordMatchScene = "Scenes/Mini Games/Word Match Mini Game";
        public const string SentenceBuilderScene = "Scenes/Mini Games/Sentence Builder Mini Game";
        public const string ListenWordScene = "Scenes/Mini Games/Listen Word Mini Game";
        public const string SightWordPopScene = "Scenes/Mini Games/Sight Word Pop Mini Game";

        public enum MiniGame
        {
            StoryQuest,
            ReadingDetective,
            StorySequencing,
            WordWizard,
            PrefixSuffix,
            RhymeTime,
            WordMatch,
            SentenceBuilder,
            ListenWord,
            SightWordPop
        }

        private const string LastPlayedKeyPrefix = "LastPlayedGame_";

        [Header("Button Target (Optional)")]
        [SerializeField] private MiniGame targetGame = MiniGame.StoryQuest;

        public static MiniGame? LastPlayed
        {
            get
            {
                string key = GetLastPlayedKey();
                if (!PlayerPrefs.HasKey(key))
                    return null;

                int value = PlayerPrefs.GetInt(key);
                return System.Enum.IsDefined(typeof(MiniGame), value) ? (MiniGame)value : null;
            }
        }

        public void LoadGame(MiniGame game)
        {
            SetLastPlayed(game);
            string sceneName = GetSceneName(game);
            SceneManager.LoadScene(sceneName);
        }

        public void LoadConfiguredGame()
        {
            LoadGame(targetGame);
        }

        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene(MainMenuScene);
        }

        public static void Load(MiniGame game)
        {
            SetLastPlayed(game);
            SceneManager.LoadScene(GetSceneName(game));
        }

        public static void BackToMenu()
        {
            SceneManager.LoadScene(MainMenuScene);
        }

        public static void SetLastPlayed(MiniGame game)
        {
            PlayerPrefs.SetInt(GetLastPlayedKey(), (int)game);
            PlayerPrefs.Save();
        }

        private static string GetLastPlayedKey()
        {
            string childId = Api.TokenStore.GetChildId();
            return string.IsNullOrEmpty(childId)
                ? LastPlayedKeyPrefix + "global"
                : LastPlayedKeyPrefix + childId;
        }

        public static string GetSceneName(MiniGame game)
        {
            return game switch
            {
                MiniGame.StoryQuest => StoryQuestScene,
                MiniGame.ReadingDetective => ReadingDetectiveScene,
                MiniGame.StorySequencing => StorySequencingScene,
                MiniGame.WordWizard => WordWizardScene,
                MiniGame.PrefixSuffix => PrefixSuffixScene,
                MiniGame.RhymeTime => RhymeTimeScene,
                MiniGame.WordMatch => WordMatchScene,
                MiniGame.SentenceBuilder => SentenceBuilderScene,
                MiniGame.ListenWord => ListenWordScene,
                MiniGame.SightWordPop => SightWordPopScene,
                _ => MainMenuScene
            };
        }
    }
}
