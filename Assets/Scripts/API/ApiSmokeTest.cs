using UnityEngine;
using Api;
using Api.Models;
using Api.Endpoints;
using System;

public class ApiSmokeTest : MonoBehaviour
{
    private ApiClient apiClient;
    private AuthApi authApi;
    private ChildApi childApi;

    private string testUsername = "smoketest_user";
    private string testPassword = "TestPass123!";

    [Header("Test Configuration")]
    [SerializeField] private bool runOnStart = false;

    void Start()
    {
        if (runOnStart)
        {
            RunTests();
        }
    }

    public async Awaitable<void> RunTests()
    {
        Debug.Log("🧪 Starting API Smoke Test...");

        try
        {
            await InitializeApi();

            await TestHealthCheck();
            await TestChildLogin();
            await TestProfileEndpoint();
            await TestStatsEndpoint();
            await TestRhymeTimeContent();
            await TestGameActivityLogging();

            Debug.Log("✅ All tests passed!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Test failed: {ex.Message}");
        }
    }

    private async Awaitable<void> InitializeApi()
    {
        Debug.Log("🔧 Initializing API client...");

        var config = ApiConfig.Create();
        config.SetDevelopment();

        apiClient = ApiClient.Instance;
        apiClient.Initialize(config);

        authApi = new AuthApi(apiClient);
        childApi = new ChildApi(apiClient);

        await Awaitable.EndOfFrameAsync();
    }

    private async Awaitable<void> TestHealthCheck()
    {
        Debug.Log("🏥 Testing health check...");

        try
        {
            string response = await apiClient.GetAsync<string>("/health");
            Debug.Log($"✅ Health check: {response}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Health check failed: {ex.Message}");
            throw;
        }

        await Awaitable.EndOfFrameAsync();
    }

    private async Awaitable<void> TestChildLogin()
    {
        Debug.Log($"👤 Testing child login ({testUsername})...");

        try
        {
            ChildAuthResponse response = await authApi.ChildLoginAsync(testUsername, testPassword);

            Debug.Log($"✅ Login successful:");
            Debug.Log($"  - Token: {response.token.Substring(0, Math.Min(20, response.token.Length))}...");
            Debug.Log($"  - Child ID: {response.childId}");
            Debug.Log($"  - Username: {response.username}");
            Debug.Log($"  - Coins: {response.coins}");
            Debug.Log($"  - Login Streak: {response.loginStreak}");

            SessionManager.Instance.SetSession(
                response.token,
                response.expiresAt,
                "Child",
                response.childId
            );
        }
        catch (ApiException ex)
        {
            if (ex.responseCode == 401)
            {
                Debug.LogWarning($"⚠️ Login failed (user may not exist): {ex.errorMessage}");
            }
            else
            {
                Debug.LogError($"❌ Login failed: {ex.errorMessage}");
                throw;
            }
        }

        await Awaitable.EndOfFrameAsync();
    }

    private async Awaitable<void> TestProfileEndpoint()
    {
        Debug.Log("👤 Testing profile endpoint...");

        try
        {
            ChildProfile profile = await childApi.GetProfileAsync();

            Debug.Log($"✅ Profile retrieved:");
            Debug.Log($"  - ID: {profile.id}");
            Debug.Log($"  - Username: {profile.username}");
            Debug.Log($"  - Coins: {profile.coins}");
            Debug.Log($"  - Login Streak: {profile.loginStreak}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Profile fetch failed: {ex.Message}");
            throw;
        }

        await Awaitable.EndOfFrameAsync();
    }

    private async Awaitable<void> TestStatsEndpoint()
    {
        Debug.Log("📊 Testing stats endpoint...");

        try
        {
            ChildStats stats = await childApi.GetStatsAsync();

            Debug.Log($"✅ Stats retrieved:");
            Debug.Log($"  - Coins: {stats.coins}");
            Debug.Log($"  - Login Streak: {stats.loginStreak}");
            Debug.Log($"  - Can Claim Daily Reward: {stats.canClaimDailyReward}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Stats fetch failed: {ex.Message}");
            throw;
        }

        await Awaitable.EndOfFrameAsync();
    }

    private async Awaitable<void> TestRhymeTimeContent()
    {
        Debug.Log("🎲 Testing RhymeTime content endpoint...");

        try
        {
            string content = await childApi.GetMiniGameContentAsync("RhymeTime");

            Debug.Log($"✅ RhymeTime content retrieved:");
            Debug.Log($"  - Content length: {content?.Length ?? 0} characters");

            if (content != null && content.Length > 0)
            {
                Debug.Log($"  - Preview: {content.Substring(0, Math.Min(100, content.Length))}...");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ RhymeTime content fetch failed: {ex.Message}");
            throw;
        }

        await Awaitable.EndOfFrameAsync();
    }

    private async Awaitable<void> TestGameActivityLogging()
    {
        Debug.Log("🎮 Testing game activity logging...");

        try
        {
            int initialCoins = int.Parse(SessionManager.Instance.ChildId != null ?
                (await childApi.GetStatsAsync()).coins.ToString() : "0");

            var gamePayload = new
            {
                gameId = "rhyme_time",
                score = 100,
                durationSeconds = 90
            };

            string payloadJson = JsonUtility.ToJson(gamePayload);

            ActivityLogged activity = await childApi.LogGameActivityAsync(payloadJson);

            Debug.Log($"✅ Activity logged:");
            Debug.Log($"  - Activity ID: {activity.id}");
            Debug.Log($"  - Activity Type: {activity.activityType}");
            Debug.Log($"  - Created At: {activity.createdAt}");

            await Awaitable.EndOfFrameAsync();

            ChildStats updatedStats = await childApi.GetStatsAsync();

            Debug.Log($"💰 Coins after activity:");
            Debug.Log($"  - Before: {initialCoins}");
            Debug.Log($"  - After: {updatedStats.coins}");
            Debug.Log($"  - Difference: {updatedStats.coins - initialCoins}");

            if (updatedStats.coins > initialCoins)
            {
                Debug.Log("✅ Coins increased as expected!");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Activity logging failed: {ex.Message}");
            throw;
        }

        await Awaitable.EndOfFrameAsync();
    }
}