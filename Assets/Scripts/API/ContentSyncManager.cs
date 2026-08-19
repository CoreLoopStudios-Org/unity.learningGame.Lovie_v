using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Api.Endpoints;

namespace Api
{
    /// <summary>
    /// Background content synchronizer that batches remote mini-game updates at startup,
    /// respecting nginx rate limits (10 req/sec) with controlled throttling.
    /// </summary>
    public class ContentSyncManager : MonoBehaviour
    {
        private static ContentSyncManager instance;
        public static ContentSyncManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("ContentSyncManager");
                    instance = go.AddComponent<ContentSyncManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private bool _isSyncing;
        private const float ThrottleIntervalSeconds = 0.15f; // ~6.6 req/sec (safely under 10 req/sec limit)

        public event Action<float> OnSyncProgress;
        public event Action OnSyncCompleted;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (SessionManager.Instance != null && SessionManager.Instance.IsAuthenticated)
            {
                StartCoroutine(SyncAllContentRoutine());
            }
        }

        public void TriggerSync()
        {
            if (!_isSyncing)
            {
                StartCoroutine(SyncAllContentRoutine());
            }
        }

        private IEnumerator SyncAllContentRoutine()
        {
            if (_isSyncing || Application.internetReachability == NetworkReachability.NotReachable)
                yield break;

            _isSyncing = true;
            Debug.Log("[ContentSyncManager] Starting background content synchronization...");

            var syncTasks = new List<(string gameType, string key)>
            {
                ("RhymeTime", null),
                ("PrefixSuffix", null),
                ("WordWizard", null),
                ("WordMatch", null),
                ("SentenceBuilder", null),
                ("WordListen", null),
                ("SightWordPop", null),
                ("StoryQuest", "sq_01"),
                ("ReadingDetective", "rd_01"),
                ("StorySequencing", "seq_01")
            };

            var wait = new WaitForSeconds(ThrottleIntervalSeconds);

            for (int i = 0; i < syncTasks.Count; i++)
            {
                var task = syncTasks[i];
                _ = ContentCacheService.SyncRemoteContentAsync(task.gameType, task.key);

                float progress = (float)(i + 1) / syncTasks.Count;
                OnSyncProgress?.Invoke(progress);

                yield return wait;
            }

            // Sync store items
            if (StoreService.Instance != null)
            {
                _ = StoreService.Instance.GetStoreItemsAsync(forceRefresh: true);
                yield return wait;
                _ = StoreService.Instance.GetMyPurchasesAsync(forceRefresh: true);
            }

            _isSyncing = false;
            Debug.Log("[ContentSyncManager] Content synchronization completed successfully.");
            OnSyncCompleted?.Invoke();
        }
    }
}
