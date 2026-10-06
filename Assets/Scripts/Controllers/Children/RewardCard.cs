using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityFigmaBridge.Runtime.UI;
using Api;

namespace UI
{
    public class RewardCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [Tooltip("Optional — e.g. \"3 / 5\". Leave unassigned if the card has no progress label.")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI claimButtonText;
        [Tooltip("Fill rect inside the fill bar — its width is scaled by progress.")]
        [SerializeField] private RectTransform progressFill;
        [SerializeField] private Button claimButton;
        [Tooltip("Figma frame that carries the claim button's fill color (auto-found as \"Frame 126\" under the button if unassigned).")]
        [SerializeField] private FigmaImage claimButtonFrame;
        [Tooltip("Claim button fill color while it can't be clicked (not complete, or already claimed).")]
        [SerializeField] private Color disabledClaimColor = new Color(0.55f, 0.55f, 0.55f, 1f);

        public RewardDefinition Definition { get; private set; }

        public event Action<RewardCard> ClaimClicked;

        private float progressFillWidth;
        private bool fillWidthCaptured;
        private Color claimButtonOriginalFillColor;

        private void Awake()
        {
            if (claimButton != null)
            {
                claimButton.onClick.AddListener(HandleClaimClicked);
            }
        }

        private void OnDestroy()
        {
            if (claimButton != null)
            {
                claimButton.onClick.RemoveListener(HandleClaimClicked);
            }
        }

        private void HandleClaimClicked()
        {
            if (Definition == null) return;
            ClaimClicked?.Invoke(this);
        }

        public void Setup(RewardDefinition definition)
        {
            Definition = definition;

            if (coinsText != null)
            {
                coinsText.text = definition != null ? definition.coinReward.ToString() : string.Empty;
            }

            if (titleText != null)
            {
                titleText.text = definition?.title ?? string.Empty;
            }

            if (subtitleText != null)
            {
                subtitleText.text = definition?.subtitle ?? string.Empty;
            }

            _ = RefreshDeferredAsync();
        }

        public async Awaitable RefreshDeferredAsync()
        {
            // Wait a frame so a freshly spawned card has a laid-out fill rect.
            await Awaitable.NextFrameAsync();
            if (this == null) return;

            Refresh();
        }

        public void Refresh()
        {
            if (Definition == null) return;

            RewardResetType type = Definition.resetType;
            int progress = RewardProgressStore.GetProgress(Definition.id, type);
            int target = Mathf.Max(1, Definition.targetCount);
            bool claimed = RewardProgressStore.IsClaimed(Definition.id, type);
            bool complete = progress >= target;

            UpdateFillBar(progress, target);

            if (progressText != null)
            {
                progressText.text = $"{Mathf.Min(progress, target)} / {target}";
            }

            if (claimButtonText != null)
            {
                claimButtonText.text = claimed ? "Claimed" : "Claim";
            }

            if (claimButton != null)
            {
                bool interactable = !claimed && complete;
                claimButton.interactable = interactable;
                ApplyClaimButtonTint(interactable);
            }
        }

        private void UpdateFillBar(int progress, int target)
        {
            if (progressFill == null) return;

            if (!fillWidthCaptured)
            {
                progressFillWidth = progressFill.rect.width;
                if (progressFillWidth <= 0f && progressFill.parent is RectTransform parent)
                {
                    progressFillWidth = parent.rect.width;
                }

                // Grow from the left edge, not the center — move the pivot to the left
                // edge and shift the position so the bar stays visually in place.
                if (progressFillWidth > 0f && !Mathf.Approximately(progressFill.pivot.x, 0f))
                {
                    progressFill.anchoredPosition += new Vector2(-progressFill.pivot.x * progressFillWidth, 0f);
                    progressFill.pivot = new Vector2(0f, progressFill.pivot.y);
                }

                fillWidthCaptured = progressFillWidth > 0f;
            }

            if (progressFillWidth <= 0f) return;

            float fraction = Mathf.Clamp01((float)progress / target);
            progressFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, progressFillWidth * fraction);
        }

        private void ApplyClaimButtonTint(bool interactable)
        {
            if (claimButtonFrame == null)
            {
                claimButtonFrame = FindClaimButtonFrame();
                if (claimButtonFrame == null) return;

                claimButtonOriginalFillColor = claimButtonFrame.FillColor;
            }

            claimButtonFrame.FillColor = interactable ? claimButtonOriginalFillColor : disabledClaimColor;
        }

        // Figma-exported name — the pill visual under the Claim Button node.
        private FigmaImage FindClaimButtonFrame()
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Frame 126" && child.GetComponent<FigmaImage>() is FigmaImage image)
                {
                    return image;
                }
            }

            return null;
        }
    }
}
