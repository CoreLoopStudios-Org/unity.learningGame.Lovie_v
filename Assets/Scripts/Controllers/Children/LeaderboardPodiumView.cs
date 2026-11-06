using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api.Models;
using Avatar;

namespace UI
{
    // Renders the top-three podium on "Main Board". Each slot shows Body/Dress/Hair/Shoe
    // plus name and score; a slot without an entry (early week / empty board) is hidden.
    public class LeaderboardPodiumView : MonoBehaviour
    {
        [Serializable]
        public class PodiumSlot
        {
            public GameObject root;
            public Image bodyImage;
            public Image hairImage;
            public Image dressImage;
            public Image shoeImage;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI scoreText;
        }

        [Header("Slots")]
        [SerializeField] private PodiumSlot firstPlace;
        [SerializeField] private PodiumSlot secondPlace;
        [SerializeField] private PodiumSlot thirdPlace;

        [Header("Avatar")]
        [SerializeField] private AvatarPartDatabase avatarDatabase;

        public void Render(IReadOnlyList<LeaderboardEntry> topEntries)
        {
            ApplySlot(firstPlace, topEntries, 0);
            ApplySlot(secondPlace, topEntries, 1);
            ApplySlot(thirdPlace, topEntries, 2);
        }

        private void ApplySlot(PodiumSlot slot, IReadOnlyList<LeaderboardEntry> topEntries, int index)
        {
            if (slot == null || slot.root == null)
                return;

            LeaderboardEntry entry = topEntries != null && index < topEntries.Count ? topEntries[index] : null;

            slot.root.SetActive(entry != null);
            if (entry == null)
                return;

            if (slot.nameText != null)
                slot.nameText.text = string.IsNullOrEmpty(entry.username) ? entry.fullName : entry.username;

            if (slot.scoreText != null)
                slot.scoreText.text = entry.score.ToString();

            AvatarSlotRenderer.Apply(
                entry.avatarState,
                AvatarSlotRenderer.ResolveDatabase(avatarDatabase),
                slot.bodyImage, slot.hairImage, slot.dressImage, slot.shoeImage);
        }
    }
}
