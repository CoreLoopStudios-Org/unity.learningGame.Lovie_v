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

        public void Setup(AdminChild child)
        {
            if (nameText != null)
            {
                nameText.text = child?.username ?? string.Empty;
            }

            if (idText != null)
            {
                idText.text = child?.id ?? string.Empty;
            }

            if (levelText != null)
            {
                levelText.text = ExtractLevel(child?.additionalData);
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
