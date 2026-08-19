using System;

namespace Api.Models
{
    [Serializable]
    public class StoreItem
    {
        public string id;
        public string name;
        public int priceInCoins;
        public string assetUrl;
        public string metadata;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    public class Purchase
    {
        public string id;
        public string childId;
        public string childUsername;
        public string storeItemId;
        public string storeItemName;
        public string storeItemAssetUrl;
        public int priceInCoins;
        public int status;
        public string requestedAt;
        public string completedAt;
        public string rejectionReason;
    }
}