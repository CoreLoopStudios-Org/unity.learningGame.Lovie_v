using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Api.Endpoints;
using Api.Models;
using Newtonsoft.Json;

namespace Api
{
    [Serializable]
    public class QueuedActivity
    {
        public string id;
        public string endpoint;
        public string payload;
        public string createdAt;
    }

    /// <summary>
    /// Queues failed activity requests to disk and automatically flushes them upon network reconnection.
    /// </summary>
    public class OfflineActivityQueue : MonoBehaviour
    {
        private static OfflineActivityQueue instance;
        public static OfflineActivityQueue Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("OfflineActivityQueue");
                    instance = go.AddComponent<OfflineActivityQueue>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private static string QueueDirectory => Path.Combine(Application.persistentDataPath, "ActivityQueue");
        private bool _isFlushing;

        public event Action<int> OnQueueCountChanged;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            if (!Directory.Exists(QueueDirectory))
            {
                Directory.CreateDirectory(QueueDirectory);
            }
        }

        private void Start()
        {
            InvokeRepeating(nameof(CheckAndFlushQueue), 5f, 15f);
        }

        public void EnqueueActivity(string endpoint, string payload)
        {
            try
            {
                var item = new QueuedActivity
                {
                    id = Guid.NewGuid().ToString(),
                    endpoint = endpoint,
                    payload = payload,
                    createdAt = DateTime.UtcNow.ToString("o")
                };

                string filePath = Path.Combine(QueueDirectory, $"{item.id}.json");
                string json = JsonConvert.SerializeObject(item);
                File.WriteAllText(filePath, json);

                Debug.Log($"[OfflineActivityQueue] Queued offline activity {item.id} for {endpoint}");
                OnQueueCountChanged?.Invoke(GetPendingCount());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OfflineActivityQueue] Failed to enqueue activity: {ex.Message}");
            }
        }

        public int GetPendingCount()
        {
            try
            {
                if (!Directory.Exists(QueueDirectory)) return 0;
                return Directory.GetFiles(QueueDirectory, "*.json").Length;
            }
            catch
            {
                return 0;
            }
        }

        public async void CheckAndFlushQueue()
        {
            if (_isFlushing || Application.internetReachability == NetworkReachability.NotReachable)
                return;

            if (SessionManager.Instance == null || !SessionManager.Instance.IsAuthenticated)
                return;

            await FlushQueueAsync();
        }

        public async Awaitable FlushQueueAsync()
        {
            if (_isFlushing || !Directory.Exists(QueueDirectory)) return;

            _isFlushing = true;
            try
            {
                string[] files = Directory.GetFiles(QueueDirectory, "*.json");
                var childApi = new ChildApi(ApiClient.Instance);

                foreach (string file in files)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var item = JsonConvert.DeserializeObject<QueuedActivity>(json);

                        if (item != null)
                        {
                            var result = await childApi.LogGameActivityAsync(item.endpoint, item.payload);
                            if (result != null)
                            {
                                File.Delete(file);
                                Debug.Log($"[OfflineActivityQueue] Successfully flushed activity {item.id}");
                            }
                        }
                        else
                        {
                            File.Delete(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[OfflineActivityQueue] Failed to flush item {file}: {ex.Message}");
                    }
                }

                OnQueueCountChanged?.Invoke(GetPendingCount());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OfflineActivityQueue] Exception flushing queue: {ex.Message}");
            }
            finally
            {
                _isFlushing = false;
            }
        }
    }
}
