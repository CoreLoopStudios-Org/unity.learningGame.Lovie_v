using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Api
{
    /// <summary>
    /// Caches downloaded sprites and multimedia assets to memory and persistent storage.
    /// </summary>
    public class RemoteAssetCache : MonoBehaviour
    {
        private static RemoteAssetCache instance;
        public static RemoteAssetCache Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("RemoteAssetCache");
                    instance = go.AddComponent<RemoteAssetCache>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private readonly Dictionary<string, Sprite> _spriteMemoryCache = new();
        private readonly Dictionary<string, AudioClip> _audioMemoryCache = new();
        private static string CacheDirectory => Path.Combine(Application.persistentDataPath, "AssetCache");

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            if (!Directory.Exists(CacheDirectory))
            {
                Directory.CreateDirectory(CacheDirectory);
            }
        }

        /// <summary>
        /// Gets or downloads a sprite from a URL, using memory and disk caching.
        /// </summary>
        public async Awaitable<Sprite> GetSpriteAsync(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            // 1. Memory cache check
            if (_spriteMemoryCache.TryGetValue(url, out var cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            string diskPath = GetDiskPath(url);

            // 2. Disk cache check
            if (File.Exists(diskPath))
            {
                try
                {
                    byte[] fileData = File.ReadAllBytes(diskPath);
                    var texture = new Texture2D(2, 2);
                    if (texture.LoadImage(fileData))
                    {
                        var sprite = Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f)
                        );
                        _spriteMemoryCache[url] = sprite;
                        return sprite;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RemoteAssetCache] Failed to load disk cached sprite: {ex.Message}");
                }
            }

            // 3. Download from network
            try
            {
                using var request = UnityWebRequestTexture.GetTexture(url);
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var texture = DownloadHandlerTexture.GetContent(request);
                    if (texture != null)
                    {
                        var sprite = Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f)
                        );

                        _spriteMemoryCache[url] = sprite;

                        // Save to disk cache
                        try
                        {
                            byte[] pngBytes = texture.EncodeToPNG();
                            if (pngBytes != null && pngBytes.Length > 0)
                            {
                                File.WriteAllBytes(diskPath, pngBytes);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[RemoteAssetCache] Failed to save sprite to disk cache: {ex.Message}");
                        }

                        return sprite;
                    }
                }
                else
                {
                    Debug.LogWarning($"[RemoteAssetCache] Failed to download sprite from '{url}': {request.error}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RemoteAssetCache] Exception downloading sprite '{url}': {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Gets or downloads an audio clip from a URL.
        /// </summary>
        public async Awaitable<AudioClip> GetAudioAsync(string url, AudioType audioType = AudioType.MPEG)
        {
            if (string.IsNullOrEmpty(url)) return null;

            if (_audioMemoryCache.TryGetValue(url, out var cachedClip) && cachedClip != null)
            {
                return cachedClip;
            }

            try
            {
                using var request = UnityWebRequestMultimedia.GetAudioClip(url, audioType);
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var clip = DownloadHandlerAudioClip.GetContent(request);
                    if (clip != null)
                    {
                        _audioMemoryCache[url] = clip;
                        return clip;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RemoteAssetCache] Failed to download audio from '{url}': {ex.Message}");
            }

            return null;
        }

        private static string GetDiskPath(string url)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(url));
            string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            return Path.Combine(CacheDirectory, $"{hash}.png");
        }
    }
}
