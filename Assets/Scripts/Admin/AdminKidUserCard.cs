using System;
using UnityEngine;
using TMPro;
using Api.Models;

namespace UI
{
    public class AdminKidUserCard : MonoBehaviour
    {
        [Serializable]
        private class ChildAdditionalData
        {
            public int level;
        }

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI idText;
        [SerializeField] private TextMeshProUGUI levelText;

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

            if (levelText != null)
            {
                levelText.text = ExtractLevel(user?.additionalData);
            }
        }

        private static string ExtractLevel(string additionalData)
        {
            if (string.IsNullOrWhiteSpace(additionalData)) return null;

            try
            {
                ChildAdditionalData data = JsonUtility.FromJson<ChildAdditionalData>(additionalData);
                return data != null && data.level > 0 ? data.level.ToString() : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
