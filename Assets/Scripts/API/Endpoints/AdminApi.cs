using Api.Models;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Api.Models
{
    [Serializable]
    public class AdminStats
    {
        public int totalParents;
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
    
    // phone/profileImageUrl are not in the backend AdminProfileDto yet —
    // JsonUtility leaves them null until the backend adds them.
    [Serializable]
    public class AdminProfile
    {
        public string id;
        public string email;
        public string fullName;
        public string phone;
        public string profileImageUrl;
        public string createdAt;
    }
    
    [Serializable]
    public class UserSummary
    {
        public string id;
        public string email;
        public string fullName;
        public int userType;
        public string disabledAt;
        public string createdAt;
        public string additionalData;
    }
    [Serializable]
    public class PaginatedUsers
    {
        public UserSummary[] users;
        public int totalCount;
        public int page;
        public int pageSize;
        public int totalPages;
    }

    [Serializable]
    public class AdminChild
    {
        public string id;
        public string username;
        public string parentId;
        public string parentName;
        public string parentEmail;
        public int coins;
        public int loginStreak;
        public string lastActivityAt;
        public string disabledAt;
        public string additionalData;
    }

    [Serializable]
    public class PaginatedChildren
    {
        public AdminChild[] children;
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

        // Backend returns the new id as a bare JSON string, not the entity.
        public async Awaitable<string> CreateStoryAsync(string title, string coverImageUrl, string contentPayload, int status)
        {
            var data = new { title, coverImageUrl, contentPayload, status };
            return await client.PostAsync<string>("/api/admin/stories", data);
        }

        public async Awaitable<bool> UpdateStoryAsync(string id, string title, string coverImageUrl, string contentPayload, int status)
        {
            var data = new { title, coverImageUrl, contentPayload, status };
            return await client.PutAsync<bool>($"/api/admin/stories/{id}", data);
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

        public async Awaitable<string> CreateQuizAsync(string storyId, string title, string questionsPayload, int status)
        {
            var data = new { storyId, title, questionsPayload, status };
            return await client.PostAsync<string>("/api/admin/quizzes", data);
        }

        public async Awaitable<bool> UpdateQuizAsync(string id, string storyId, string title, string questionsPayload, int status)
        {
            var data = new { storyId, title, questionsPayload, status };
            return await client.PutAsync<bool>($"/api/admin/quizzes/{id}", data);
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

        public async Awaitable<string> CreateStoreItemAsync(string name, int priceInCoins, string assetUrl, string metadata)
        {
            var data = new { name, priceInCoins, assetUrl, metadata };
            return await client.PostAsync<string>("/api/admin/store-items", data);
        }

        public async Awaitable<bool> UpdateStoreItemAsync(string id, string name, int priceInCoins, string assetUrl, string metadata)
        {
            var data = new { name, priceInCoins, assetUrl, metadata };
            return await client.PutAsync<bool>($"/api/admin/store-items/{id}", data);
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
        
        // Backend create DTO requires contentPayload (no status at create).
        public async Awaitable<string> CreateMiniGameContentAsync(string gameType, string title, string contentPayload)
        {
            var data = new { gameType, title, contentPayload };
            return await client.PostAsync<string>("/api/admin/minigames", data);
        }

        // Backend answers { "success": true } for update and delete.
        public async Awaitable<bool> UpdateMiniGameContentAsync(string id, string gameType, string title, int status)
        {
            var data = new { gameType, title, status };
            await client.PutAsync<object>($"/api/admin/minigames/{id}", data);
            return true;
        }

        public async Awaitable<bool> DeleteMiniGameContentAsync(string id)
        {
            await client.DeleteAsync<object>($"/api/admin/minigames/{id}");
            return true;
        }

        // StoryAudio CRUD — route is /api/admin/storyaudio (no hyphen).
        public async Awaitable<StoryAudio[]> GetStoryAudiosByStoryAsync(string storyId)
        {
            return await client.GetAsync<StoryAudio[]>($"/api/admin/storyaudio/story/{storyId}");
        }

        public async Awaitable<StoryAudio> GetStoryAudioAsync(string id)
        {
            return await client.GetAsync<StoryAudio>($"/api/admin/storyaudio/{id}");
        }

        public async Awaitable<StoryAudio> CreateStoryAudioAsync(string storyId, int type, string audioUrl)
        {
            var data = new { storyId, audioUrl, type };
            return await client.PostAsync<StoryAudio>("/api/admin/storyaudio", data);
        }

        public async Awaitable<StoryAudio> UpdateStoryAudioAsync(string id, int type, string audioUrl)
        {
            var data = new { audioUrl, type };
            return await client.PutAsync<StoryAudio>($"/api/admin/storyaudio/{id}", data);
        }

        // Backend answers 200 with an empty body.
        public async Awaitable<bool> DeleteStoryAudioAsync(string id)
        {
            await client.DeleteAsync<object>($"/api/admin/storyaudio/{id}");
            return true;
        }

        // Users
        public async Awaitable<PaginatedUsers> GetUsersAsync(int page = 1, int pageSize = 10, string search = null, string sortBy = null, bool descending = false)
        {
            string url = $"/api/admin/users?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(search)) url += $"&searchTerm={search}";
            if (!string.IsNullOrEmpty(sortBy)) url += $"&sortBy={sortBy}&sortDescending={descending.ToString().ToLower()}";
            return await client.GetAsync<PaginatedUsers>(url);
        }

        // Children are NOT users — they live in a separate table, so this
        // endpoint (not GetUsersAsync) is the only source of kid accounts.
        public async Awaitable<PaginatedChildren> GetChildrenAsync(int page = 1, int pageSize = 10, string search = null, string sortBy = null, bool descending = false)
        {
            string url = $"/api/admin/children?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(search)) url += $"&searchTerm={search}";
            if (!string.IsNullOrEmpty(sortBy)) url += $"&sortBy={sortBy}&sortDescending={descending.ToString().ToLower()}";
            return await client.GetAsync<PaginatedChildren>(url);
        }

        // Backend route is PATCH and answers 204 No Content.
        public async Awaitable<bool> DisableUserAsync(string id, bool disable)
        {
            var data = new { disabled = disable };
            await client.PatchAsync<object>($"/api/admin/users/{id}/disable", data);
            return true;
        }

        public async Awaitable<bool> DisableChildAsync(string id, bool disable)
        {
            var data = new { disabled = disable };
            await client.PatchAsync<object>($"/api/admin/children/{id}/disable", data);
            return true;
        }

        public async Awaitable<bool> DeleteUserAsync(string id)
        {
            await client.DeleteAsync<object>($"/api/admin/users/{id}");
            return true;
        }

        public async Awaitable<bool> DeleteChildAsync(string id)
        {
            await client.DeleteAsync<object>($"/api/admin/children/{id}");
            return true;
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
