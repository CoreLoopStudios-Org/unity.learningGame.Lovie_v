using UnityEngine;

namespace Api
{
    // Nothing else sets Application.targetFrameRate, and Unity defaults to 30fps on
    // Android — the app reads as laggy on modern high-refresh screens without this.
    internal static class AppBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            Application.targetFrameRate = 60;
        }
    }
}
