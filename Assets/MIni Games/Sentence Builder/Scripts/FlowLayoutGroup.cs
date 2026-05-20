using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoreLoop.SentenceBuilder
{
    /// <summary>
    /// Wrapping flow layout: arranges children left-to-right and breaks to a new row
    /// when the container width is exceeded. Respects each child's preferred size
    /// (works with children that have VerticalLayoutGroup + TMP, no ContentSizeFitter needed).
    /// childAlignment controls both horizontal row alignment and vertical child alignment
    /// within each row — it works exactly as in Unity's built-in layout groups.
    /// </summary>
    [AddComponentMenu("Layout/Flow Layout Group")]
    [RequireComponent(typeof(RectTransform))]
    public class FlowLayoutGroup : LayoutGroup
    {
        [SerializeField] private float _spacingX = 8f;
        [SerializeField] private float _spacingY = 8f;

        public float SpacingX { get => _spacingX; set { _spacingX = value; SetDirty(); } }
        public float SpacingY { get => _spacingY; set { _spacingY = value; SetDirty(); } }

        // ── ILayoutElement ────────────────────────────────────────────────────────

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            // Min width = padding + the widest single child (so at least one item fits per row)
            float minW = padding.horizontal;
            foreach (var child in rectChildren)
                minW = Mathf.Max(minW, LayoutUtility.GetMinWidth(child) + padding.horizontal);
            SetLayoutInputForAxis(minW, -1f, -1f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            float h = ComputeHeight(rectTransform.rect.width);
            SetLayoutInputForAxis(h, h, -1f, 1);
        }

        // ── ILayoutGroup ──────────────────────────────────────────────────────────

        // Everything is placed in SetLayoutVertical so both axes are available.
        public override void SetLayoutHorizontal() { }
        public override void SetLayoutVertical()   => Arrange(rectTransform.rect.width);

        // ── Layout logic ─────────────────────────────────────────────────────────

        private void Arrange(float containerW)
        {
            BuildRows(containerW, out var rows);
            float usableW = containerW - padding.horizontal;
            float y       = padding.top;

            for (int r = 0; r < rows.Count; r++)
            {
                var   row  = rows[r];
                float rowW = RowWidth(row);
                float rowH = RowHeight(row);
                float x    = padding.left + HAlignOffset(rowW, usableW);

                foreach (var child in row)
                {
                    float w = LayoutUtility.GetPreferredWidth(child);
                    float h = LayoutUtility.GetPreferredHeight(child);
                    SetChildAlongAxis(child, 0, x,                    w);
                    SetChildAlongAxis(child, 1, y + VAlignOffset(rowH, h), h);
                    x += w + _spacingX;
                }

                y += rowH + (r < rows.Count - 1 ? _spacingY : 0f);
            }
        }

        private float ComputeHeight(float containerW)
        {
            BuildRows(containerW, out var rows);
            if (rows.Count == 0) return padding.vertical;

            float h = padding.vertical;
            for (int r = 0; r < rows.Count; r++)
            {
                h += RowHeight(rows[r]);
                if (r < rows.Count - 1) h += _spacingY;
            }
            return h;
        }

        private void BuildRows(float containerW, out List<List<RectTransform>> rows)
        {
            rows = new List<List<RectTransform>>();
            if (rectChildren.Count == 0) return;

            float usableW = containerW - padding.horizontal;
            var   row     = new List<RectTransform>();
            float rowW    = 0f;

            foreach (var child in rectChildren)
            {
                float childW = LayoutUtility.GetPreferredWidth(child);
                float needed = row.Count == 0 ? childW : rowW + _spacingX + childW;

                if (row.Count > 0 && needed > usableW)
                {
                    rows.Add(row);
                    row   = new List<RectTransform>();
                    rowW  = 0f;
                    needed = childW;
                }

                row.Add(child);
                rowW = needed;
            }

            if (row.Count > 0) rows.Add(row);
        }

        // Total width of items in a row (including inter-item spacing).
        private float RowWidth(List<RectTransform> row)
        {
            float w = 0f;
            for (int i = 0; i < row.Count; i++)
            {
                w += LayoutUtility.GetPreferredWidth(row[i]);
                if (i < row.Count - 1) w += _spacingX;
            }
            return w;
        }

        private static float RowHeight(List<RectTransform> row)
        {
            float h = 0f;
            foreach (var c in row) h = Mathf.Max(h, LayoutUtility.GetPreferredHeight(c));
            return h;
        }

        // Horizontal offset for the row start based on childAlignment.
        private float HAlignOffset(float rowW, float usableW)
        {
            switch (childAlignment)
            {
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    return Mathf.Max(0f, (usableW - rowW) * 0.5f);

                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    return Mathf.Max(0f, usableW - rowW);

                default: // Left
                    return 0f;
            }
        }

        // Vertical offset for a child within its row based on childAlignment.
        private float VAlignOffset(float rowH, float childH)
        {
            switch (childAlignment)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.UpperCenter:
                case TextAnchor.UpperRight:
                    return 0f;

                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    return rowH - childH;

                default: // Middle
                    return (rowH - childH) * 0.5f;
            }
        }
    }
}
