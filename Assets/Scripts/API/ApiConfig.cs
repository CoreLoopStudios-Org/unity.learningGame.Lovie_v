using UnityEngine;

namespace Api
{
    [CreateAssetMenu(fileName = "ApiConfig", menuName = "API/Config")]
    public class ApiConfig : ScriptableObject
    {
        public const string DevelopmentBaseUrl = "http://localhost:5200";
        public const string ProductionBaseUrl = "https://imaginemebylovie.com";

        private static ApiConfig instance;
        public static ApiConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<ApiConfig>("ApiConfig");
                    if (instance == null)
                    {
                        instance = Create();
                    }
                }
                return instance;
            }
        }

        [Header("Configuration")]
        [SerializeField] private string baseUrl = DevelopmentBaseUrl;

        [Header("Remote Content")]
        [SerializeField] private bool useRemoteContent = false;

        public string BaseUrl => baseUrl;
        public bool UseRemoteContent => useRemoteContent;

        public void SetDevelopment()
        {
            baseUrl = DevelopmentBaseUrl;
        }

        public void SetProduction()
        {
            baseUrl = ProductionBaseUrl;
        }

        public void SetUseRemoteContent(bool enable)
        {
            useRemoteContent = enable;
        }

        public static ApiConfig Create()
        {
            var config = CreateInstance<ApiConfig>();
            config.SetDevelopment();
            return config;
        }
    }
}