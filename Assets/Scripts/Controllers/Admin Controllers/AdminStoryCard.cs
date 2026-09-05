using System;
using System.Globalization;
using UnityEngine;
using TMPro;
using Api.Models;

namespace UI
{
    public class AdminStoryCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI uploadedText;

        public void Setup(Story story)
        {
            if (nameText != null)
            {
                nameText.text = story?.title ?? string.Empty;
            }

            if (uploadedText != null)
            {
                uploadedText.text = FormatUploadedDate(story?.updatedAt, story?.createdAt);
            }
        }

        private static string FormatUploadedDate(string updatedAt, string createdAt)
        {
            string raw = string.IsNullOrWhiteSpace(updatedAt) ? createdAt : updatedAt;
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime date))
            {
                return date.ToLocalTime().ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
            }

            return raw;
        }
    }
}
