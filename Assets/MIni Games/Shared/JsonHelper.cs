using System;
using UnityEngine;

/// <summary>
/// Utility for deserializing a top-level JSON array using Unity's JsonUtility, which only
/// supports deserializing objects, not arrays, at the root level. Wraps the array in a
/// throwaway object before delegating to JsonUtility.
/// Shared across any mini-game whose content is authored as a JSON array (e.g. RhymeTime's
/// flat pair list). Story Quest does not use this — its content is one object per file.
/// </summary>
public static class JsonHelper
{
    #region Public Methods

    /// <summary>
    /// Deserializes a JSON array string into an array of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="json">A JSON string whose root element is an array, e.g. "[ {...}, {...} ]".</param>
    /// <returns>The deserialized array, or an empty array if parsing fails.</returns>
    public static T[] FromJsonArray<T>(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[JsonHelper] FromJsonArray called with null or empty json string.");
            return Array.Empty<T>();
        }

        string wrapped = "{\"items\":" + json + "}";

        try
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
            return wrapper?.items ?? Array.Empty<T>();
        }
        catch (Exception e)
        {
            Debug.LogError($"[JsonHelper] Failed to parse JSON array: {e.Message}");
            return Array.Empty<T>();
        }
    }

    #endregion

    #region Private Methods

    [Serializable]
    private class Wrapper<T>
    {
        public T[] items;
    }

    #endregion
}