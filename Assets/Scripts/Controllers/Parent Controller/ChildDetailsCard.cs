using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;
using Avatar;

namespace UI
{
    public class ChildDetailsCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Button selectButton;

        [Header("Avatar")]
        [SerializeField] private Image bodyImage;
        [SerializeField] private Image hairImage;
        [SerializeField] private Image dressImage;
        [SerializeField] private AvatarPartDatabase avatarDatabase;

        private ChildListItem currentChild;
        private Action<ChildListItem> onSelect;
        private ParentApi parentApi;
        private int requestId;

        private void Awake()
        {
            if (selectButton == null) selectButton = GetComponent<Button>();
            if (selectButton != null) selectButton.onClick.AddListener(OnSelectClicked);

            ApiClient apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            parentApi = new ParentApi(apiClient);
        }

        private void OnDestroy()
        {
            if (selectButton != null) selectButton.onClick.RemoveListener(OnSelectClicked);
        }

        public void Setup(ChildListItem child, Action<ChildListItem> onSelect)
        {
            currentChild = child;
            this.onSelect = onSelect;

            if (nameText != null)
                nameText.text = string.IsNullOrEmpty(child?.fullName) ? child?.username ?? string.Empty : child.fullName;

            int id = ++requestId;
            _ = LoadAvatarAsync(child?.id, id);
        }

        // The children list DTO carries no avatarState — fetch it from the child detail endpoint.
        private async Awaitable LoadAvatarAsync(string childId, int id)
        {
            if (string.IsNullOrEmpty(childId))
                return;

            try
            {
                ChildDetail detail = await parentApi.GetChildAsync(childId);

                if (id != requestId || this == null)
                    return;

                AvatarSlotRenderer.Apply(detail?.avatarState, ResolveDatabase(), bodyImage, hairImage, dressImage);
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    Debug.LogWarning($"[ChildDetailsCard] Failed to load avatar for {childId}: {ex.Message}");
            }
        }

        private AvatarPartDatabase ResolveDatabase()
        {
            if (avatarDatabase != null)
                return avatarDatabase;

            var manager = FindObjectOfType<AvatarCustomizationManager>();
            return manager != null ? manager.Database : null;
        }

        private void OnSelectClicked()
        {
            if (currentChild != null) onSelect?.Invoke(currentChild);
        }
    }
}
