using UnityEngine;

namespace MiniGames
{
    [CreateAssetMenu(fileName = "MiniGameInfo_", menuName = "Lovie/Mini Game Info")]
    public class MiniGameInfo : ScriptableObject
    {
        [Header("Card Fields")]
        [SerializeField] private string displayName;
        [SerializeField] private string subtitle;
        [SerializeField] private Sprite icon;
        [SerializeField] private int coins;

        [Header("Target")]
        [SerializeField] private UI.MiniGameNavigator.MiniGame game;

        public string DisplayName => displayName;
        public string Subtitle => subtitle;
        public Sprite Icon => icon;
        public int Coins => coins;
        public UI.MiniGameNavigator.MiniGame Game => game;
    }
}
