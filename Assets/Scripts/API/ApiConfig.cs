using UnityEngine;

namespace Api
{
    [CreateAssetMenu(fileName = "ApiConfig", menuName = "API/Config")]
    public class ApiConfig : ScriptableObject
    {
        public const string LocalDevUrl = "http://localhost:5201";
        public const string RemoteDevUrl = "https://dev-api.imaginemebylovie.com";
        public const string ProductionUrl = "https://api.imaginemebylovie.com";

        public enum EnvironmentType { Local, RemoteDev, Production, Auto }

        [Header("Environment")]
        [Tooltip("Auto will use RemoteDev in the Unity Editor and Production in release builds.")]
        [SerializeField] private EnvironmentType currentEnvironment = EnvironmentType.Auto;

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

        [Header("Remote Content")]
        [SerializeField] private bool useRemoteContent = false;

        public bool UseRemoteContent => useRemoteContent;

        public string BaseUrl 
        {
            get 
            {
                if (currentEnvironment == EnvironmentType.Auto)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    return RemoteDevUrl;
#else
                    return ProductionUrl;
#endif
                }

                switch (currentEnvironment)
                {
                    case EnvironmentType.Local: return LocalDevUrl;
                    case EnvironmentType.RemoteDev: return RemoteDevUrl;
                    case EnvironmentType.Production: return ProductionUrl;
                    default: return ProductionUrl;
                }
            }
        }

        public void SetDevelopment()
        {
            currentEnvironment = EnvironmentType.RemoteDev;
        }

        public void SetProduction()
        {
            currentEnvironment = EnvironmentType.Production;
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