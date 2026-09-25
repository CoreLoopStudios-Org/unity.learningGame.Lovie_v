using System;
using System.Collections.Generic;
using Api.Models;
using UnityEngine;

namespace Api.Models
{
    // GET /api/parent/profile — ParentProfileDto (API doc §5.8)
    [Serializable]
    public class ParentProfile
    {
        public string id;
        public string fullName;
        public string email;
        public string profilePictureUrl;
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

        // PUT /api/parent/profile — every field is optional; nulls are omitted so
        // only the supplied values are updated.
        public async Awaitable<bool> UpdateProfileAsync(string fullName = null, string email = null, string profilePictureUrl = null, string currentPassword = null, string newPassword = null)
        {
            var data = new Dictionary<string, string>();
            if (fullName != null) data["fullName"] = fullName;
            if (email != null) data["email"] = email;
            if (profilePictureUrl != null) data["profilePictureUrl"] = profilePictureUrl;
            if (currentPassword != null) data["currentPassword"] = currentPassword;
            if (newPassword != null) data["newPassword"] = newPassword;
            return await client.PutAsync<bool>("/api/parent/profile", data);
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