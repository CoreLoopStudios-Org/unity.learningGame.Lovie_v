using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Api
{
    public enum RewardResetType
    {
        None,
        Daily
    }

    [Serializable]
    internal class RewardRecord
    {
        // rewardId, or "rewardId:yyyyMMdd" for Daily rewards — day-scoped keys make
        // midnight rollover implicit (yesterday's bucket is simply never read again).
        public string key;
        public int progress;
    }

    [Serializable]
    internal class RewardClaimRecord
    {
        public string rewardId;
        public bool claimed;
        public string claimedDayKey;
    }

    [Serializable]
    internal class RewardProgressData
    {
        public List<RewardRecord> progress = new List<RewardRecord>();
        public List<RewardClaimRecord> claims = new List<RewardClaimRecord>();
    }

    /// <summary>
    /// Mission progress lives client-side (the backend has no mission endpoints), keyed per
    /// child. Game code reports from anywhere: RewardProgressStore.ReportProgress("quiz_complete").
    /// The id is free-form — unknown ids are recorded harmlessly, so triggers never depend on
    /// the rewards panel or the RewardCatalog existing. Daily rewards pass RewardResetType.Daily.
    /// </summary>
    public static class RewardProgressStore
    {
        private const string KeyPrefix = "lovie.rewardprogress.";

        public static event Action<string> OnProgressChanged;

        public static string GetStorageKey(string childId)
        {
            return KeyPrefix + (string.IsNullOrEmpty(childId) ? "default" : childId);
        }

        public static void ReportProgress(string rewardId, int amount = 1, RewardResetType type = RewardResetType.None)
        {
            if (string.IsNullOrEmpty(rewardId) || amount == 0) return;

            string childId = CurrentChildId();
            RewardProgressData data = Load(childId);
            string key = ProgressKey(rewardId, type);
            RewardRecord record = data.progress.Find(r => r.key == key);
            if (record == null)
            {
                record = new RewardRecord { key = key };
                data.progress.Add(record);
            }

            record.progress += amount;
            Save(childId, data);

            OnProgressChanged?.Invoke(rewardId);
        }

        public static int GetProgress(string rewardId, RewardResetType type = RewardResetType.None)
        {
            if (string.IsNullOrEmpty(rewardId)) return 0;

            RewardProgressData data = Load(CurrentChildId());
            RewardRecord record = data.progress.Find(r => r.key == ProgressKey(rewardId, type));
            return record?.progress ?? 0;
        }

        public static bool IsComplete(string rewardId, int targetCount, RewardResetType type = RewardResetType.None)
        {
            return GetProgress(rewardId, type) >= Mathf.Max(1, targetCount);
        }

        public static bool IsClaimed(string rewardId, RewardResetType type = RewardResetType.None)
        {
            if (string.IsNullOrEmpty(rewardId)) return false;

            RewardClaimRecord claim = Load(CurrentChildId()).claims.Find(c => c.rewardId == rewardId);
            if (claim == null) return false;

            return type == RewardResetType.Daily
                ? claim.claimedDayKey == TodayKey()
                : claim.claimed;
        }

        public static void MarkClaimed(string rewardId, RewardResetType type = RewardResetType.None)
        {
            if (string.IsNullOrEmpty(rewardId)) return;

            string childId = CurrentChildId();
            RewardProgressData data = Load(childId);
            RewardClaimRecord claim = data.claims.Find(c => c.rewardId == rewardId);
            if (claim == null)
            {
                claim = new RewardClaimRecord { rewardId = rewardId };
                data.claims.Add(claim);
            }

            if (type == RewardResetType.Daily)
            {
                claim.claimedDayKey = TodayKey();
            }
            else
            {
                claim.claimed = true;
            }

            Save(childId, data);

            OnProgressChanged?.Invoke(rewardId);
        }

        private static string ProgressKey(string rewardId, RewardResetType type)
        {
            return type == RewardResetType.Daily ? $"{rewardId}:{TodayKey()}" : rewardId;
        }

        private static string TodayKey()
        {
            return DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }

        private static string CurrentChildId()
        {
            return SessionManager.Instance != null ? SessionManager.Instance.ChildId : null;
        }

        private static RewardProgressData Load(string childId)
        {
            string json = PlayerPrefs.GetString(GetStorageKey(childId), string.Empty);
            if (string.IsNullOrEmpty(json)) return new RewardProgressData();

            try
            {
                return JsonUtility.FromJson<RewardProgressData>(json) ?? new RewardProgressData();
            }
            catch
            {
                return new RewardProgressData();
            }
        }

        private static void Save(string childId, RewardProgressData data)
        {
            PlayerPrefs.SetString(GetStorageKey(childId), JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
