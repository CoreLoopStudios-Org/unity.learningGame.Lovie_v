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
        public int totalChildren;
        public int totalStories;
        public int totalQuizzes;
        public int totalStoreItems;
        public int totalActivities;
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
        public async Awaitable<Story[]> GetStoriesAsync()
        {
            return await client.GetAsync<Story[]>("/api/admin/stories");
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
        public async Awaitable<StoreItem[]> GetStoreItemsAsync()
        {
            return await client.GetAsync<StoreItem[]>("/api/admin/store-items");
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

        // MiniGame Content CRUD
        public async Awaitable<MiniGameContent[]> GetMiniGameContentsAsync()
        {
            return await client.GetAsync<MiniGameContent[]>("/api/admin/minigames");
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
        public async Awaitable<UserSummary[]> GetUsersAsync()
        {
            return await client.GetAsync<UserSummary[]>("/api/admin/users");
        }

        public async Awaitable<bool> DisableUserAsync(string id, bool disable)
        {
            var data = new { disable };
            return await client.PostAsync<bool>($"/api/admin/users/{id}/disable", data);
        }
    }
}
