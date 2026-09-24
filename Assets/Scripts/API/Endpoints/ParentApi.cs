using System;
using Api.Models;
using UnityEngine;

namespace Api.Models
{
    // /api/parent/profile is not in the backend yet (admin profile exists,
    // parent profile still 404s) — JsonUtility leaves fields null until the
    // backend adds the endpoint. Password is never returned by the API.
    [Serializable]
    public class ParentProfile
    {
        public string id;
        public string username;
        public string email;
        public string fullName;
        public string profileImageUrl;
        public string createdAt;
    }
}

namespace Api.Endpoints
{
    public class ParentApi
    {
        private readonly ApiClient client;

        public ParentApi(ApiClient apiClient)
        {
            client = apiClient;
        }

        public async Awaitable<ParentProfile> GetProfileAsync()
        {
            return await client.GetAsync<ParentProfile>("/api/parent/profile");
        }

        public async Awaitable<ParentDashboard> GetDashboardAsync()
        {
            return await client.GetAsync<ParentDashboard>("/api/parent/dashboard");
        }

        public async Awaitable<string> CreateChildAsync(string fullName, string username, string password)
        {
            var data = new
            {
                fullName,
                username,
                password
            };

            return await client.PostAsync<string>("/api/parent/children", data);
        }

        public async Awaitable<ChildListItem[]> GetChildrenAsync()
        {
            return await client.GetAsync<ChildListItem[]>("/api/parent/children");
        }

        public async Awaitable<ChildDetail> GetChildAsync(string id)
        {
            return await client.GetAsync<ChildDetail>($"/api/parent/children/{id}");
        }

        public async Awaitable<bool> UpdateChildAsync(string id, string username = null, string password = null, string avatarState = null, string additionalData = null)
        {
            var data = new
            {
                username,
                password,
                avatarState,
                additionalData
            };

            return await client.PutAsync<bool>($"/api/parent/children/{id}", data);
        }

        public async Awaitable<bool> DeleteChildAsync(string id)
        {
            return await client.DeleteAsync<bool>($"/api/parent/children/{id}");
        }

        public async Awaitable<ChildActivity[]> GetChildActivitiesAsync(string id)
        {
            return await client.GetAsync<ChildActivity[]>($"/api/parent/children/{id}/activities");
        }
    }
}