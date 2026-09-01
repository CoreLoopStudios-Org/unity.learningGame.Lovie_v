using UnityEngine;
using TMPro;
using Api.Models;

namespace UI
{
    public class AdminParentUserCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI idText;

        public void Setup(UserSummary user)
        {
            if (nameText != null)
            {
                nameText.text = user?.fullName ?? string.Empty;
            }

            if (idText != null)
            {
                idText.text = user?.id ?? string.Empty;
            }
        }
    }
}
