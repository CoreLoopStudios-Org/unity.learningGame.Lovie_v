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
    public int priceInCoins; // Always >= 0. 0 = Free, >0 = Paid (backend rejects negatives with 400)
    public bool isUnlocked; // Free stories are always true. Admin API responses are always true (admins bypass pricing).
    
    public string createdAt;
    public string updatedAt;
}
```

---

## 2. Rendering the Story UI
The endpoint `GET /api/child/stories` now returns **all** published stories. You must use the `isUnlocked` flag to determine how the UI looks. Use `priceInCoins == 0` to detect a free story — never infer pricing from `isUnlocked`.

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

> **Admin panel note:** admin story endpoints (`GET /api/admin/stories`) always return `isUnlocked: true`. If your admin UI shows a Free/Paid badge, read `priceInCoins > 0` — showing every story as "Free" because `isUnlocked` is true is a client-side bug, not a backend one.

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
            // Backend rejected the purchase. Reasons: not enough coins, already owned,
            // story is free (priceInCoins <= 0), or story is not Published.
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
    public string storeProductId; // The product ID configured in the Admin Panel matching Apple/Google
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

    // The Unity Product ID should exactly match the StoreItem.StoreProductId in the backend
    _ = SendReceiptToBackendAsync(unityProductId, appleOrGoogleTransactionId);

    return PurchaseProcessingResult.Complete;
}

private async Task SendReceiptToBackendAsync(string storeProductId, string transactionId)
{
    var requestData = new ProcessIapRequest 
    { 
        storeProductId = storeProductId, 
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

