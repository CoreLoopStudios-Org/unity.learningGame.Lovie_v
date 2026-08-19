using System;
using System.IO;
using UnityEngine;
using Api.Endpoints;

namespace Api
{
    /// <summary>
    /// Disk-caching service for remote mini-game content payloads.
    /// Saves successful payloads to Application.persistentDataPath/ContentCache,
    /// falls back to cached files when offline, and falls back to bundled Resources if never cached.
    /// </summary>
    public static class ContentCacheService
    {
        private static string CacheDirectory => Path.Combine(Application.persistentDataPath, "ContentCache");

        public static void SaveToCache(string key, string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json)) return;

                if (!Directory.Exists(CacheDirectory))
                {
                    Directory.CreateDirectory(CacheDirectory);
                }

                string filePath = Path.Combine(CacheDirectory, $"{SanitizeFileName(key)}.json");
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ContentCacheService] Failed to cache '{key}': {ex.Message}");
            }
        }

        public static bool TryGetFromCache(string key, out string json)
        {
            try
            {
                string filePath = Path.Combine(CacheDirectory, $"{SanitizeFileName(key)}.json");
                if (File.Exists(filePath))
                {
                    json = File.ReadAllText(filePath);
                    if (!string.IsNullOrEmpty(json))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ContentCacheService] Failed to read cached '{key}': {ex.Message}");
            }

            json = null;
            return false;
        }

        public static string GetResourcesFallback(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            return asset != null ? asset.text : null;
        }

        /// <summary>
        /// Attempts to sync content from backend API. Updates disk cache on success.
        /// </summary>
        public static async Awaitable<string> SyncRemoteContentAsync(string gameType, string key = null)
        {
            try
            {
                var apiClient = ApiClient.Instance;
                if (apiClient == null) return null;

                var childApi = new ChildApi(apiClient);
                string json = await childApi.GetMiniGameContentAsync(gameType, key);

                if (!string.IsNullOrEmpty(json))
                {
                    string cacheKey = string.IsNullOrEmpty(key) ? gameType : $"{gameType}_{key}";
                    SaveToCache(cacheKey, json);
                    return json;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ContentCacheService] Sync failed for {gameType}{(key != null ? $"/{key}" : "")}: {ex.Message}");
            }

            return null;
        }

        private static string SanitizeFileName(string key)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                key = key.Replace(c, '_');
            }
            return key;
        }
    }
}
