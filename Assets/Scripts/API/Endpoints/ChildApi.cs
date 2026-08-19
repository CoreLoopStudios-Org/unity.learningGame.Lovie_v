using Api.Models;
using System.Threading.Tasks;

namespace Api.Endpoints
{
    public class ChildApi
    {
        private readonly ApiClient client;

        public ChildApi(ApiClient apiClient)
        {
            client = apiClient;
        }

        public async Task Awaitable<ChildProfile> GetProfileAsync()
        {
            return await client.GetAsync<ChildProfile>("/api/child/profile");
        }

        public async Task Awaitable<ChildStats> GetStatsAsync()
        {
            return await client.GetAsync<ChildStats>("/api/child/stats");
        }

        public async Task Awaitable<bool> UpdateAvatarAsync(string avatarState)
        {
            var data = new { avatarState };
            return await client.PutAsync<bool>("/api/child/avatar", data);
        }

        public async Task Awaitable<DailyRewardResult> ClaimDailyRewardAsync()
        {
            return await client.PostAsync<DailyRewardResult>("/api/child/daily-reward", new { });
        }

        public async Task Awaitable<Story[]> GetStoriesAsync()
        {
            return await client.GetAsync<Story[]>("/api/child/stories");
        }

        public async Task Awaitable<Story> GetStoryAsync(string id)
        {
            return await client.GetAsync<Story>($"/api/child/stories/{id}");
        }

        public async Task Awaitable<Quiz[]> GetQuizzesAsync(string storyId = null)
        {
            string endpoint = "/api/child/quizzes";
            if (!string.IsNullOrEmpty(storyId))
                endpoint += $"?storyId={storyId}";

            return await client.GetAsync<Quiz[]>(endpoint);
        }

        public async Task Awaitable<Quiz> GetQuizAsync(string id)
        {
            return await client.GetAsync<Quiz>($"/api/child/quizzes/{id}");
        }

        public async Task Awaitable<ActivityLogged> LogStoryActivityAsync(string storyId, string payload)
        {
            var data = new { storyId, payload };
            return await client.PostAsync<ActivityLogged>("/api/child/activities/story", data);
        }

        public async Task Awaitable<ActivityLogged> LogQuizActivityAsync(string quizId, string payload)
        {
            var data = new { quizId, payload };
            return await client.PostAsync<ActivityLogged>("/api/child/activities/quiz", data);
        }

        public async Task Awaitable<ActivityLogged> LogGameActivityAsync(string payload)
        {
            var data = new { payload };
            return await client.PostAsync<ActivityLogged>("/api/child/activities/game", data);
        }

        public async Task Awaitable<StoreItem[]> GetStoreItemsAsync()
        {
            return await client.GetAsync<StoreItem[]>("/api/child/store/items");
        }

        public async Task Awaitable<Purchase> PurchaseItemAsync(string storeItemId)
        {
            var data = new { storeItemId };
            return await client.PostAsync<Purchase>("/api/child/store/purchase", data);
        }

        public async Task Awaitable<Purchase[]> GetMyItemsAsync()
        {
            return await client.GetAsync<Purchase[]>("/api/child/store/my-items");
        }

        public async Task Awaitable<MiniGameContent[]> GetMiniGamesAsync(string gameType = null)
        {
            string endpoint = "/api/child/minigames";
            if (!string.IsNullOrEmpty(gameType))
                endpoint += $"?gameType={gameType}";

            return await client.GetAsync<MiniGameContent[]>(endpoint);
        }

        public async Task Awaitable<MiniGameContent> GetMiniGameAsync(string id)
        {
            return await client.GetAsync<MiniGameContent>($"/api/child/minigames/{id}");
        }

        public async Task Awaitable<string> GetMiniGameContentAsync(string gameType, string key = null)
        {
            string endpoint = $"/api/child/minigames/content/{gameType}";
            if (!string.IsNullOrEmpty(key))
                endpoint += $"?key={key}";

            return await client.GetAsync<string>(endpoint);
        }
    }
}