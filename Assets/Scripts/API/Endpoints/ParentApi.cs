using Api.Models;
using UnityEngine;

namespace Api.Endpoints
{
    public class ParentApi
    {
        private readonly ApiClient client;

        public ParentApi(ApiClient apiClient)
        {
            client = apiClient;
        }

        public async Awaitable<ParentDashboard> GetDashboardAsync()
        {
            return await client.GetAsync<ParentDashboard>("/api/parent/dashboard");
        }

        public async Awaitable<string> CreateChildAsync(string username, string password)
        {
            var data = new
            {
                username,
                password
            };

            return await client.PostAsync<string>("/api/parent/children", data);
        }

        public async Awaitable<ChildSummary[]> GetChildrenAsync()
        {
            return await client.GetAsync<ChildSummary[]>("/api/parent/children");
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