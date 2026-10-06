using System;
using System.Collections.Generic;
using UnityEngine;
using Api;

namespace UI
{
    [Serializable]
    public class RewardDefinition
    {
        [Tooltip("Unique progress key — game code reports via RewardProgressStore.ReportProgress(id).")]
        public string id;

        public string title;

        [TextArea]
        public string subtitle;

        public int coinReward;

        [Min(1)]
        public int targetCount = 1;

        public RewardResetType resetType;
    }

    [CreateAssetMenu(fileName = "RewardCatalog", menuName = "Rewards/Reward Catalog")]
    public class RewardCatalog : ScriptableObject
    {
        [SerializeField] private List<RewardDefinition> rewards = new List<RewardDefinition>();

        public List<RewardDefinition> Rewards => rewards;

        public RewardDefinition Find(string id)
        {
            return string.IsNullOrEmpty(id) ? null : rewards.Find(r => r != null && r.id == id);
        }
    }
}
