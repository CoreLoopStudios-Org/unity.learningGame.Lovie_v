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
            lineImage.rectTransform.pivot = new Vector2(0, 0.5f);
        }

        public void SetColor(Color color)
        {
            lineImage.color = color;
        }

        public void UpdateLine(Vector2 startPos, Vector2 endPos)
        {
            Vector2 direction = endPos - startPos;
            float distance = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            lineImage.rectTransform.position = startPos;
            lineImage.rectTransform.sizeDelta = new Vector2(distance, lineWidth);
            lineImage.rectTransform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
