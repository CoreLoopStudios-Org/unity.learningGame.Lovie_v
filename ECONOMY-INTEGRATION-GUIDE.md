# Unity Implementation Guide: Stories & IAP

This guide provides the necessary C# code snippets and logic flows for the Unity frontend to integrate the newly added **Story Monetization** and **In-App Purchases (IAP)**.

---

## 1. Update your DTOs
First, update your local `StoryDto` model to match the backend's new properties.

```csharp
[System.Serializable]
public class StoryDto
{
    public string id;
    public string title;
    public string coverImageUrl;
    public string contentPayload;
    public int status;
    
    // NEW FIELDS:
    public int priceInCoins; 
    public bool isUnlocked; 
    
    public string createdAt;
    public string updatedAt;
}
```

---

## 2. Rendering the Story UI
The endpoint `GET /api/child/stories` now returns **all** published stories. You must use the `isUnlocked` flag to determine how the UI looks.

```csharp
public void PopulateStoryCard(StoryDto story)
{
    titleText.text = story.title;
    // Load coverImageUrl...

    if (story.isUnlocked)
    {
        // The child owns this (or it's free)
        actionButtonText.text = "Read";
        actionButton.onClick.AddListener(() => OpenStory(story.id));
        coinIcon.SetActive(false);
    }
    else
    {
        // The child does not own this yet
        actionButtonText.text = $"Buy: {story.priceInCoins}";
        coinIcon.SetActive(true);
        actionButton.onClick.AddListener(() => PromptPurchase(story));
    }
}
```

---

## 3. Purchasing a Story (Coins -> Story)
When the child clicks "Buy", call the new purchase endpoint. Ensure you wrap this in a `try/catch` to handle backend database constraints (e.g., if they try to buy something they can't afford).

```csharp
[System.Serializable]
public class PurchaseStoryResponse { public bool success; } // Based on backend returning a bool

public async Task BuyStoryAsync(StoryDto story)
{
    // Prevent UI spam
    buyButton.interactable = false;

    try
    {
        string url = $"{API_BASE_URL}/api/child/stories/{story.id}/purchase";
        
        // Assuming ApiClient has a PostAsync helper:
        var response = await ApiClient.PostAsync<bool>(url, null);

        if (response)
        {
            Debug.Log("Story unlocked successfully!");
            // 1. Deduct coins locally in the UI
            ChildStore.UpdateCoins(-story.priceInCoins);
            // 2. Change the button to "Read"
            story.isUnlocked = true;
            PopulateStoryCard(story);
        }
    }
    catch (ApiException ex)
    {
        if (ex.StatusCode == 400)
        {
            // Backend DB prevented the purchase (Not enough coins, or already owned)
            ShowErrorDialog("Purchase failed. Not enough coins or already unlocked.");
        }
    }
    finally
    {
        buyButton.interactable = true;
    }
}
```

---

## 4. Processing Real In-App Purchases (Money -> Coins)
When the parent completes a purchase via Apple/Google (using Unity IAP), you must send the receipt/transaction ID to the backend to get the coins.

### Step 4A: Define the Request Model
```csharp
[System.Serializable]
public class ProcessIapRequest
{
    public string tierId; // The Guid of the IapTier configured in the Admin Panel
    public string transactionId; // The unique ID from Apple/Google
}
```

### Step 4B: Call the API from the Unity IAP Callback
```csharp
using UnityEngine.Purchasing; // Unity IAP package

public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
{
    string appleOrGoogleTransactionId = args.purchasedProduct.transactionID;
    string unityProductId = args.purchasedProduct.definition.id;

    // Map your Unity Product ID to the Backend's Tier GUID
    string backendTierGuid = GetTierGuidForProduct(unityProductId); 

    // Fire and forget the async API call
    _ = SendReceiptToBackendAsync(backendTierGuid, appleOrGoogleTransactionId);

    return PurchaseProcessingResult.Complete;
}

private async Task SendReceiptToBackendAsync(string tierId, string transactionId)
{
    var requestData = new ProcessIapRequest 
    { 
        tierId = tierId, 
        transactionId = transactionId 
    };

    try
    {
        string url = $"{API_BASE_URL}/api/child/store/iap/process";
        
        // Backend returns an int representing the NEW total coin balance
        int newTotalCoins = await ApiClient.PostAsync<int>(url, requestData);
        
        // Update the UI Header with the shiny new coins!
        ChildStore.SetTotalCoins(newTotalCoins);
        ShowSuccessEffect("Coins Added!");
    }
    catch (ApiException ex)
    {
        if (ex.StatusCode == 400)
        {
            // The backend Unique Index caught a duplicate transaction ID.
            // This happens if the network dropped and Unity retried the receipt.
            // Treat it as safe/already handled.
            Debug.LogWarning("Receipt was already processed by the server.");
        }
        else
        {
            ShowErrorDialog("Failed to verify purchase. Please contact support.");
        }
    }
}
```

### Important IAP Mapping Note
The `GetTierGuidForProduct()` method is up to your Unity architecture. You can hardcode a dictionary mapping `com.imagineme.coins.small` to the Guid `3fa85f64-...` generated in the Admin dashboard, or you can fetch the available tiers from a future Admin endpoint and cache them.
