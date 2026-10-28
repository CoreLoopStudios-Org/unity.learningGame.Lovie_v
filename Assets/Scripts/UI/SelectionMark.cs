using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Generic selection indicator. Attach to any selectable object and assign
    /// the visual to toggle (defaults to the first child Image). Any system can
    /// drive it with SetSelected; it knows nothing about what selects it.
    /// </summary>
    public class SelectionMark : MonoBehaviour
    {
        [SerializeField] private GameObject selectedVisual;

        public bool IsSelected { get; private set; }

        private void Awake()
        {
            ResolveVisual();
            SetSelected(false);
        }

        private void OnEnable()
        {
            ApplyVisual();
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            ApplyVisual();
        }

        private void ResolveVisual()
        {
            if (selectedVisual != null)
                return;

            foreach (Transform child in transform)
            {
                if (child.GetComponent<Image>() != null)
                {
                    selectedVisual = child.gameObject;
                    return;
                }
            }

            Debug.LogWarning($"[{name}] SelectionMark has no visual assigned and no child Image found");
        }

        private void ApplyVisual()
        {
            if (selectedVisual != null)
            {
                selectedVisual.SetActive(IsSelected);
            }
        }
    }
}
