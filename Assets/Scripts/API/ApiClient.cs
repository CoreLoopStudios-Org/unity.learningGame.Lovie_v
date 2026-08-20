using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;

namespace Api
{
    public class ApiClient
    {
        private static ApiClient instance;
        public static ApiClient Instance => instance ??= new ApiClient();

        private ApiConfig config;
        private const int TimeoutSeconds = 30;
        private const int MaxRetries = 2;

        public event Action OnSessionExpired;

        public void Initialize(ApiConfig apiConfig)
        {
            config = apiConfig;
        }

        public async Awaitable<T> GetAsync<T>(string endpoint, int retryCount = 0)
        {
            string url = $"{config.BaseUrl}{endpoint}";
            using UnityWebRequest request = UnityWebRequest.Get(url);

            return await SendRequestAsync<T>(request, retryCount);
        }

        public async Awaitable<T> PostAsync<T>(string endpoint, object data, int retryCount = 0)
        {
            string url = $"{config.BaseUrl}{endpoint}";
            using UnityWebRequest request = new UnityWebRequest(url, "POST");

            string json = JsonConvert.SerializeObject(data);
            byte[] body = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            return await SendRequestAsync<T>(request, retryCount);
        }

        public async Awaitable<T> PutAsync<T>(string endpoint, object data, int retryCount = 0)
        {
            string url = $"{config.BaseUrl}{endpoint}";
            using UnityWebRequest request = new UnityWebRequest(url, "PUT");

            string json = JsonConvert.SerializeObject(data);
            byte[] body = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            return await SendRequestAsync<T>(request, retryCount);
        }

        public async Awaitable<T> DeleteAsync<T>(string endpoint, int retryCount = 0)
        {
            string url = $"{config.BaseUrl}{endpoint}";
            using UnityWebRequest request = UnityWebRequest.Delete(url);
            request.downloadHandler = new DownloadHandlerBuffer(); // Fix D5: DELETE needs download handler for error body

            return await SendRequestAsync<T>(request, retryCount);
        }

        private async Awaitable<T> SendRequestAsync<T>(UnityWebRequest request, int retryCount)
        {
            request.timeout = TimeoutSeconds;

            AttachAuthHeader(request);

            try
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;

                    if (typeof(T) == typeof(string))
                        return (T)(object)responseText;

                    return JsonConvert.DeserializeObject<T>(responseText);
                }
                else
                {
                    return await HandleErrorAsync<T>(request, retryCount);
                }
            }
            catch (Exception ex)
            {
                throw new ApiException(500, $"Request failed: {ex.Message}");
            }
        }

        private void AttachAuthHeader(UnityWebRequest request)
        {
            string token = SessionManager.Instance?.Token;

            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", $"Bearer {token}");
            }
        }

        private async Awaitable<T> HandleErrorAsync<T>(UnityWebRequest request, int retryCount)
        {
            if (request.responseCode == 401)
            {
                OnSessionExpired?.Invoke();
                throw new ApiException(401, "Session expired");
            }

            if (retryCount < MaxRetries && IsRetryableError(request.responseCode))
            {
                int delay = (int)Math.Pow(2, retryCount) * 1000;
                await Task.Delay(delay);

                string method = request.method;
                string url = request.url;

                if (method == "GET")
                    return await GetAsync<T>(url.Replace(config.BaseUrl, ""), retryCount + 1);
            }

            string errorResponse = request.downloadHandler?.text ?? "{}";

            try
            {
                var error = JsonConvert.DeserializeObject<ApiErrorResponse>(errorResponse);
                throw new ApiException(error);
            }
            catch
            {
                throw new ApiException((int)request.responseCode, errorResponse);
            }
        }

        private bool IsRetryableError(long responseCode)
        {
            return responseCode == 408 || responseCode == 429 || responseCode >= 500;
        }
    }
}