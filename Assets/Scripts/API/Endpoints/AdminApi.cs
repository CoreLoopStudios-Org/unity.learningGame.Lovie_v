using Api.Models;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Api.Models
{
    [Serializable]
    public class AdminStats
    {
        public int totalUsers;
        public int activeChildren;
        public int totalStories;
        public int totalQuizzes;
        public int totalStoreItems;
        public int totalActivities;
        public TopContent[] mostWatchedStories;
        public TopContent[] mostPlayedGames;
        public int totalEarnings;
    }
    
    [Serializable]
    public class TopContent
    {
        public string name;
        public string category;
        public string thumbnailUrl;
    }
    
    [Serializable]
    public class AdminProfile
    {
        public string id;
        public string email;
        public string fullName;
        public string createdAt;
    }
    
    [Serializable]
    public class UserSummary
    {
        public string id;
        public string email;
        public string fullName;
        public int userType;
        public bool isDisabled;
        public string createdAt;
    }
    [Serializable]
    public class PaginatedUsers
    {
        public UserSummary[] items;
        public int totalCount;
        public int page;
        public int pageSize;
        public int totalPages;
    }
}

namespace Api.Endpoints
{
    public class AdminApi
    {
        private readonly ApiClient client;

        public AdminApi(ApiClient apiClient)
        {
            client = apiClient;
        }

        public async Awaitable<AdminStats> GetStatsAsync()
        {
            return await client.GetAsync<AdminStats>("/api/admin/stats");
        }

        // Stories CRUD
        public async Awaitable<Story[]> GetStoriesAsync(string sortBy = null, int? status = null, string titleSearch = null)
        {
            string url = "/api/admin/stories?";
            if (!string.IsNullOrEmpty(sortBy)) url += $"sortBy={sortBy}&";
            if (status.HasValue) url += $"status={status}&";
            if (!string.IsNullOrEmpty(titleSearch)) url += $"titleSearch={titleSearch}&";
            return await client.GetAsync<Story[]>(url.TrimEnd('?', '&'));
        }

        public async Awaitable<Story[]> GetRecentStoriesAsync()
        {
            return await client.GetAsync<Story[]>("/api/admin/stories/recent");
        }

        public async Awaitable<Story> GetStoryAsync(string id)
        {
            return await client.GetAsync<Story>($"/api/admin/stories/{id}");
        }

        public async Awaitable<Story> CreateStoryAsync(string title, string coverImageUrl, string contentPayload, int status)
        {
            var data = new { title, coverImageUrl, contentPayload, status };
            return await client.PostAsync<Story>("/api/admin/stories", data);
        }

        public async Awaitable<Story> UpdateStoryAsync(string id, string title, string coverImageUrl, string contentPayload, int status)
        {
            var data = new { title, coverImageUrl, contentPayload, status };
            return await client.PutAsync<Story>($"/api/admin/stories/{id}", data);
        }

        public async Awaitable<bool> DeleteStoryAsync(string id)
        {
            return await client.DeleteAsync<bool>($"/api/admin/stories/{id}");
        }

        // Quizzes CRUD
        public async Awaitable<Quiz[]> GetQuizzesAsync()
        {
            return await client.GetAsync<Quiz[]>("/api/admin/quizzes");
        }

        public async Awaitable<Quiz> GetQuizAsync(string id)
        {
            return await client.GetAsync<Quiz>($"/api/admin/quizzes/{id}");
        }

        public async Awaitable<Quiz> CreateQuizAsync(string storyId, string title, string questionsPayload, int status)
        {
            var data = new { storyId, title, questionsPayload, status };
            return await client.PostAsync<Quiz>("/api/admin/quizzes", data);
        }

        public async Awaitable<Quiz> UpdateQuizAsync(string id, string storyId, string title, string questionsPayload, int status)
        {
            var data = new { storyId, title, questionsPayload, status };
            return await client.PutAsync<Quiz>($"/api/admin/quizzes/{id}", data);
        }

        public async Awaitable<bool> DeleteQuizAsync(string id)
        {
            return await client.DeleteAsync<bool>($"/api/admin/quizzes/{id}");
        }

        // Store Items CRUD
        public async Awaitable<StoreItem[]> GetStoreItemsAsync(int? minPrice = null, int? maxPrice = null, string nameSearch = null)
        {
            string url = "/api/admin/store-items?";
            if (minPrice.HasValue) url += $"minPrice={minPrice}&";
            if (maxPrice.HasValue) url += $"maxPrice={maxPrice}&";
            if (!string.IsNullOrEmpty(nameSearch)) url += $"nameSearch={nameSearch}&";
            return await client.GetAsync<StoreItem[]>(url.TrimEnd('?', '&'));
        }

        public async Awaitable<StoreItem> GetStoreItemAsync(string id)
        {
            return await client.GetAsync<StoreItem>($"/api/admin/store-items/{id}");
        }

        public async Awaitable<StoreItem> CreateStoreItemAsync(string name, int priceInCoins, string assetUrl, string metadata)
        {
            var data = new { name, priceInCoins, assetUrl, metadata };
            return await client.PostAsync<StoreItem>("/api/admin/store-items", data);
        }

        public async Awaitable<StoreItem> UpdateStoreItemAsync(string id, string name, int priceInCoins, string assetUrl, string metadata)
        {
            var data = new { name, priceInCoins, assetUrl, metadata };
            return await client.PutAsync<StoreItem>($"/api/admin/store-items/{id}", data);
        }

        public async Awaitable<bool> DeleteStoreItemAsync(string id)
        {
            return await client.DeleteAsync<bool>($"/api/admin/store-items/{id}");
        }

        public async Awaitable<StoreItem> AddStoryToStoreAsync(string storyId)
        {
            return await client.PostAsync<StoreItem>($"/api/admin/store-items/story/{storyId}", new { });
        }

        // MiniGame Content CRUD
        public async Awaitable<MiniGameContent[]> GetMiniGameContentsAsync()
        {
            return await client.GetAsync<MiniGameContent[]>("/api/admin/minigames");
        }
        
        public async Awaitable<MiniGameContent> GetMiniGameContentAsync(string id)
        {
            return await client.GetAsync<MiniGameContent>($"/api/admin/minigames/{id}");
        }
        
        public async Awaitable<MiniGameContent> CreateMiniGameContentAsync(string gameType, string title, int status)
        {
            var data = new { gameType, title, status };
            return await client.PostAsync<MiniGameContent>("/api/admin/minigames", data);
        }

        public async Awaitable<MiniGameContent> UpdateMiniGameContentAsync(string id, string gameType, string title, int status)
        {
            var data = new { gameType, title, status };
            return await client.PutAsync<MiniGameContent>($"/api/admin/minigames/{id}", data);
        }

        public async Awaitable<bool> DeleteMiniGameContentAsync(string id)
        {
            return await client.DeleteAsync<bool>($"/api/admin/minigames/{id}");
        }

        // StoryAudio CRUD
        public async Awaitable<StoryAudio[]> GetStoryAudiosAsync()
        {
            return await client.GetAsync<StoryAudio[]>("/api/admin/story-audio");
        }
        
        public async Awaitable<StoryAudio> GetStoryAudioAsync(string id)
        {
            return await client.GetAsync<StoryAudio>($"/api/admin/story-audio/{id}");
        }
        
        public async Awaitable<StoryAudio> CreateStoryAudioAsync(string storyId, int audioType, string audioUrl)
        {
            var data = new { storyId, audioType, audioUrl };
            return await client.PostAsync<StoryAudio>("/api/admin/story-audio", data);
        }

        public async Awaitable<StoryAudio> UpdateStoryAudioAsync(string id, string storyId, int audioType, string audioUrl)
        {
            var data = new { storyId, audioType, audioUrl };
            return await client.PutAsync<StoryAudio>($"/api/admin/story-audio/{id}", data);
        }

        public async Awaitable<bool> DeleteStoryAudioAsync(string id)
        {
            return await client.DeleteAsync<bool>($"/api/admin/story-audio/{id}");
        }

        // Users
        public async Awaitable<PaginatedUsers> GetUsersAsync(int page = 1, int pageSize = 10)
        {
            return await client.GetAsync<PaginatedUsers>($"/api/admin/users?page={page}&pageSize={pageSize}");
        }

        public async Awaitable<bool> DisableUserAsync(string id, bool disable)
        {
            var data = new { disable };
            return await client.PostAsync<bool>($"/api/admin/users/{id}/disable", data);
        }

        // Profile
        public async Awaitable<AdminProfile> GetProfileAsync()
        {
            return await client.GetAsync<AdminProfile>("/api/admin/profile");
        }

        public async Awaitable<bool> UpdateCredentialsAsync(string email, string currentPassword, string newPassword)
        {
            var data = new { email, currentPassword, newPassword };
            return await client.PutAsync<bool>("/api/admin/profile/credentials", data);
        }

        // Media Upload
        public async Awaitable<string> UploadMediaAsync(byte[] fileData, string fileName)
        {
            var form = new WWWForm();
            form.AddBinaryData("file", fileData, fileName);
            
            // The backend returns { "url": "/uploads/..." }
            var response = await client.PostFormAsync<System.Collections.Generic.Dictionary<string, string>>("/api/admin/media/upload", form);
            return response != null && response.ContainsKey("url") ? response["url"] : null;
        }
    }
}
