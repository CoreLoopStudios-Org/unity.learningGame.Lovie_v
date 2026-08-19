using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class MiniGameNavigator : MonoBehaviour
    {
        public const string MainMenuScene = "Main Game/Children/Main Menu";
        public const string StoryQuestScene = "Mini Games/Story Quest Mini Game";
        public const string ReadingDetectiveScene = "Mini Games/Reading Detective Mini Game";
        public const string StorySequencingScene = "Mini Games/Story Sequencing Mini Game";
        public const string WordWizardScene = "Mini Games/Word Wizard mini Game";
        public const string PrefixSuffixScene = "Mini Games/Prefix Suffix Mini";
        public const string RhymeTimeScene = "Mini Games/Rhyme Time Mini Game";
        public const string WordMatchScene = "Mini Games/Word Match Mini Game";
        public const string SentenceBuilderScene = "Mini Games/Sentence Builder Mini Game";
        public const string ListenWordScene = "Mini Games/Listen Word Mini Game";
        public const string SightWordPopScene = "Mini Games/Sight Word Pop Mini Game";

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

        [Header("Button Target (Optional)")]
        [SerializeField] private MiniGame targetGame = MiniGame.StoryQuest;

        public void LoadGame(MiniGame game)
        {
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
            SceneManager.LoadScene(GetSceneName(game));
        }

        public static void BackToMenu()
        {
            SceneManager.LoadScene(MainMenuScene);
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
