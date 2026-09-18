using UnityEngine;

namespace MiniGames
{
    public class GamesPageController : MonoBehaviour
    {
        [Header("Catalog (index 0 = default game)")]
        [SerializeField] private MiniGameInfo[] catalog;
        [SerializeField] private GameCardView lastPlayedCard;

        private void OnEnable()
        {
            RefreshLastPlayedCard();
        }

        public void RefreshLastPlayedCard()
        {
            if (lastPlayedCard == null || catalog == null || catalog.Length == 0)
                return;

            MiniGameInfo shown = FindInfo(UI.MiniGameNavigator.LastPlayed) ?? catalog[0];
            lastPlayedCard.Bind(shown);
        }

        private MiniGameInfo FindInfo(UI.MiniGameNavigator.MiniGame? game)
        {
            if (game == null)
                return null;

            foreach (MiniGameInfo info in catalog)
            {
                if (info != null && info.Game == game.Value)
                    return info;
            }

            return null;
        }
    }
}
