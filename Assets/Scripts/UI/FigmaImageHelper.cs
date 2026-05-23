using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

namespace UI
{
    /// <summary>
    /// Helper class for accessing Figma image properties like Stroke Width.
    /// Use this to modify Figma-imported image properties at runtime.
    /// </summary>
    public static class FigmaImageHelper
    {
        /// <summary>
        /// Sets the stroke width on a Figma-imported Image component.
        /// </summary>
        /// <param name="image">The Image component from Figma import</param>
        /// <param name="strokeWidth">Stroke width value (0 = no stroke, 4+ = visible stroke)</param>
        public static void SetStrokeWidth(Image image, float strokeWidth)
        {
            if (image == null)
                return;

            // Try to find StrokeWidth property using reflection
            SetPropertyViaReflection(image, "StrokeWidth", strokeWidth);
            SetPropertyViaReflection(image, "strokeWidth", strokeWidth);
            SetPropertyViaReflection(image, "_StrokeWidth", strokeWidth);
        }

        /// <summary>
        /// Gets the stroke width from a Figma-imported Image component.
        /// </summary>
        public static float GetStrokeWidth(Image image)
        {
            if (image == null)
                return 0f;

            // Try to get StrokeWidth property
            float value = GetPropertyViaReflection<float>(image, "StrokeWidth");
            if (value > 0)
                return value;

            value = GetPropertyViaReflection<float>(image, "strokeWidth");
            if (value > 0)
                return value;

            return 0f;
        }

        /// <summary>
        /// Sets the stroke color on a Figma-imported Image component.
        /// </summary>
        public static void SetStrokeColor(Image image, Color color)
        {
            if (image == null)
                return;

            SetPropertyViaReflection(image, "StrokeColor", color);
            SetPropertyViaReflection(image, "strokeColor", color);
            SetPropertyViaReflection(image, "_StrokeColor", color);
        }

        private static void SetPropertyViaReflection(object target, string propertyName, object value)
        {
            try
            {
                PropertyInfo property = target.GetType().GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (property != null && property.CanWrite)
                {
                    property.SetValue(target, value);
                }
                else
                {
                    // Try as Field
                    FieldInfo field = target.GetType().GetField(propertyName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (field != null)
                    {
                        field.SetValue(target, value);
                    }
                }
            }
            catch
            {
                // Property or field not found or not accessible
            }
        }

        private static T GetPropertyViaReflection<T>(object target, string propertyName)
        {
            try
            {
                PropertyInfo property = target.GetType().GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (property != null && property.CanRead)
                {
                    object value = property.GetValue(target);
                    if (value is T typedValue)
                        return typedValue;
                }

                // Try as Field
                FieldInfo field = target.GetType().GetField(propertyName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (field != null)
                {
                    object value = field.GetValue(target);
                    if (value is T typedValue)
                        return typedValue;
                }
            }
            catch
            {
                // Property or field not found
            }

            return default(T);
        }
    }
}
