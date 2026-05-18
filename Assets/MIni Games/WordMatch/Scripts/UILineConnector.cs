using UnityEngine;
using UnityEngine.UI;

namespace CoreLoop.WordMatch
{
    public class UILineConnector : MonoBehaviour
    {
        [SerializeField] private Image lineImage;
        [SerializeField] private float lineWidth = 5f;

        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (lineImage != null)
            {
                lineImage.rectTransform.pivot = new Vector2(0, 0.5f);
            }
        }

        public void SetColor(Color color)
        {
            if (lineImage != null)
            {
                lineImage.color = color;
            }
        }

        public void UpdateLine(Vector3 startPos, Vector3 endPos)
        {
            if (lineImage == null) return;

            Vector3 direction = endPos - startPos;
            float distance = direction.magnitude;
            
            // Fix for Canvas scaling: Convert world distance to local sizeDelta
            float scale = lineImage.rectTransform.lossyScale.x;
            float localDistance = scale > 0 ? distance / scale : distance;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            lineImage.rectTransform.position = startPos;
            lineImage.rectTransform.sizeDelta = new Vector2(localDistance, lineWidth);
            lineImage.rectTransform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
