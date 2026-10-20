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

        [Header("Social Sign-In (native Google/Apple)")]
        [Tooltip("Google 'Web application' OAuth client ID. Android Credential Manager uses it as the server client ID, so Android ID tokens carry it as their audience.")]
        [SerializeField] private string googleServerClientId;

        [Tooltip("Google 'iOS' OAuth client ID. Drives the iOS OAuth flow and its ID token audience; must also be listed in the backend's Authentication__Google__ClientIds.")]
        [SerializeField] private string googleIosClientId;

        /// <summary>Web client ID — Android Credential Manager serverClientId. Empty means Google sign-in is unavailable.</summary>
        public string GoogleServerClientId => googleServerClientId;

        /// <summary>iOS OAuth client ID used by the native PKCE flow. Empty means Google sign-in is unavailable on iOS.</summary>
        public string GoogleIosClientId => googleIosClientId;

        public string BaseUrl 
        {
            get 
            {
//                 if (currentEnvironment == EnvironmentType.Auto)
//                 {
// #if UNITY_EDITOR || DEVELOPMENT_BUILD
//                     return RemoteDevUrl;
// #else
//                     return ProductionUrl;
// #endif
//                 }
//
//                 switch (currentEnvironment)
//                 {
//                     case EnvironmentType.Local: return LocalDevUrl;
//                     case EnvironmentType.RemoteDev: return RemoteDevUrl;
//                     case EnvironmentType.Production: return ProductionUrl;
//                     default: return ProductionUrl;
//                 }
                return RemoteDevUrl;
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