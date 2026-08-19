using UnityEngine;

namespace Api
{
    [CreateAssetMenu(fileName = "ApiConfig", menuName = "API/Config")]
    public class ApiConfig : ScriptableObject
    {
        public const string DevelopmentBaseUrl = "http://localhost:5200";
        public const string ProductionBaseUrl = "https://imaginemebylovie.com";

        [Header("Configuration")]
        [SerializeField] private string baseUrl = DevelopmentBaseUrl;

        public string BaseUrl => baseUrl;

        public void SetDevelopment()
        {
            baseUrl = DevelopmentBaseUrl;
        }

        public void SetProduction()
        {
            baseUrl = ProductionBaseUrl;
        }

        public static ApiConfig Create()
        {
            var config = CreateInstance<ApiConfig>();
            config.SetDevelopment();
            return config;
        }
    }
}