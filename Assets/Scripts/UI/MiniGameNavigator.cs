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
            RequestGamesPageOnLoad();
            SceneManager.LoadScene(GetSceneName(game));
        }

        public void LoadConfiguredGame()
        {
            LoadGame(targetGame);
        }

        public void ReturnToMainMenu()
        {
            BackToMenu();
        }

        public static void Load(MiniGame game)
        {
            SetLastPlayed(game);
            RequestGamesPageOnLoad();
            SceneManager.LoadScene(GetSceneName(game));
        }

        public static void BackToMenu()
        {
            // Back tapped after finishing but before Continue — don't let the
            // DontDestroyOnLoad completion overlay persist into the menu.
            var completion = FindFirstObjectByType<Api.GameCompletionService>();
            if (completion != null)
            {
                completion.HideCompletionPanel();
            }

            RequestGamesPageOnLoad();
            SceneManager.LoadScene(MainMenuScene);
        }

        // The menu always opens on Home otherwise; every minigame exit should land
        // back on the Games page.
        private static void RequestGamesPageOnLoad()
        {
            AdminNavigationController.PendingPage = AdminPageType.Games;
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
                // The two scene files contain each other's managers (the Word Wizard
                // scene runs the Word-Listen game and vice versa), so the card routes
                // are crossed until the scenes themselves are fixed.
                MiniGame.WordWizard => ListenWordScene,
                MiniGame.PrefixSuffix => PrefixSuffixScene,
                MiniGame.RhymeTime => RhymeTimeScene,
                MiniGame.WordMatch => WordMatchScene,
                MiniGame.SentenceBuilder => SentenceBuilderScene,
                MiniGame.ListenWord => WordWizardScene,
                MiniGame.SightWordPop => SightWordPopScene,
                _ => MainMenuScene
            };
        }
    }
}
