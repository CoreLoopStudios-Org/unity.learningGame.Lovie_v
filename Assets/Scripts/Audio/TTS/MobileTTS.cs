using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Cross-platform offline text-to-speech: Android TextToSpeech, iOS AVSpeechSynthesizer.
    /// Self-creates and persists across scenes; safe to call from any UI code.
    /// Speak() interrupts any current utterance; calls made before the engine
    /// finishes initializing are queued. In the editor it stays silent (warns once).
    /// </summary>
    public class MobileTTS : MonoBehaviour
    {
        public const string EnglishUS = "en-US";

        private static MobileTTS instance;
        // Same rule as AudioManager: no lazy re-creation mid-teardown.
        private static bool recreationSuppressed;

        public static MobileTTS Instance
        {
            get
            {
                if (instance == null && !recreationSuppressed)
                {
                    GameObject go = new GameObject("MobileTTS");
                    instance = go.AddComponent<MobileTTS>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private enum NativeBackend { Unsupported, Android, Ios }

        private NativeBackend backend = NativeBackend.Unsupported;
        private AndroidJavaObject androidBridge;
        private bool nativeReady;
        private bool unavailableWarned;
        private string pendingSpeak;
        private string activeLanguage = EnglishUS;
        private float speechRate = 1f;

        /// <summary>True once the native engine finished initializing.</summary>
        public bool IsReady => backend != NativeBackend.Unsupported && nativeReady;

        /// <summary>True while an utterance is currently being spoken.</summary>
        public bool IsSpeaking
        {
            get
            {
                if (!nativeReady) return false;
#if UNITY_ANDROID && !UNITY_EDITOR
                return androidBridge != null && androidBridge.Call<bool>("isSpeaking");
#elif UNITY_IOS && !UNITY_EDITOR
                return LovieTTS_IsSpeaking();
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID && !UNITY_EDITOR
            backend = NativeBackend.Android;
            InitAndroid();
#elif UNITY_IOS && !UNITY_EDITOR
            backend = NativeBackend.Ios;
            LovieTTS_Init();
            nativeReady = true;
            ApplyLanguageNative(activeLanguage);
            SetSpeechRateNative(speechRate);
#else
            backend = NativeBackend.Unsupported;
            Debug.Log("MobileTTS: native TTS is only available on Android/iOS device builds; Speak() calls will be ignored here.", this);
#endif
        }

        /// <summary>Speaks the text, interrupting any current utterance.</summary>
        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            if (backend == NativeBackend.Unsupported)
            {
                if (!unavailableWarned)
                {
                    unavailableWarned = true;
                    Debug.LogWarning("MobileTTS: no native TTS on this platform — Speak() ignored.", this);
                }
                return;
            }

            if (!nativeReady)
            {
                pendingSpeak = text;
                return;
            }

            SpeakNative(text);
        }

        public void Stop()
        {
            if (!nativeReady)
            {
                pendingSpeak = null;
                return;
            }

            StopNative();
        }

        /// <summary>BCP-47 tag, e.g. "en-US". Returns false when the language is
        /// unavailable on the device (device default voice is used as fallback).</summary>
        public bool SetLanguage(string languageTag)
        {
            if (string.IsNullOrWhiteSpace(languageTag)) return false;

            activeLanguage = languageTag;

            // Queued natively until init completes; validated once applied.
            if (backend == NativeBackend.Unsupported || !nativeReady) return true;

            return ApplyLanguageNative(languageTag);
        }

        /// <summary>1.0 = normal, 0.5 = half speed, 2.0 = double speed.</summary>
        public void SetSpeechRate(float rate)
        {
            speechRate = Mathf.Clamp(rate <= 0f ? 1f : rate, 0.25f, 3f);

            if (backend == NativeBackend.Unsupported || !nativeReady) return;

            SetSpeechRateNative(speechRate);
        }

        private void Update()
        {
            if (backend != NativeBackend.Android || nativeReady) return;

            if (androidBridge == null || !androidBridge.Call<bool>("isReady")) return;

            nativeReady = true;
            ApplyLanguageNative(activeLanguage);
            SetSpeechRateNative(speechRate);

            if (!string.IsNullOrEmpty(pendingSpeak))
            {
                string text = pendingSpeak;
                pendingSpeak = null;
                SpeakNative(text);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Stop();
        }

        private void OnDestroy()
        {
            if (instance != this) return;

            instance = null;
            recreationSuppressed = true;

            ShutdownNative();

#if UNITY_ANDROID && !UNITY_EDITOR
            if (androidBridge != null)
            {
                androidBridge.Dispose();
                androidBridge = null;
            }
#endif
        }

        private void InitAndroid()
        {
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaClass bridgeClass = new AndroidJavaClass("com.lovie.tts.TTSBridge"))
                {
                    androidBridge = bridgeClass.CallStatic<AndroidJavaObject>("getInstance", activity);
                }
            }
            catch (Exception e)
            {
                backend = NativeBackend.Unsupported;
                Debug.LogError($"MobileTTS: failed to start Android TextToSpeech: {e.Message}", this);
            }
        }

        private void SpeakNative(string text)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            androidBridge?.Call("speak", text);
#elif UNITY_IOS && !UNITY_EDITOR
            LovieTTS_Speak(text);
#endif
        }

        private void StopNative()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            androidBridge?.Call("stop");
#elif UNITY_IOS && !UNITY_EDITOR
            LovieTTS_Stop();
#endif
        }

        private bool ApplyLanguageNative(string languageTag)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            int result = androidBridge.Call<int>("setLanguage", languageTag);
            if (result < 0)
            {
                Debug.LogWarning($"MobileTTS: language '{languageTag}' unavailable on this device (code {result}); using device default.", this);
                return false;
            }
            return true;
#elif UNITY_IOS && !UNITY_EDITOR
            if (LovieTTS_SetLanguage(languageTag) != 0)
            {
                Debug.LogWarning($"MobileTTS: no iOS voice installed for '{languageTag}'; using default voice.", this);
                return false;
            }
            return true;
#else
            return false;
#endif
        }

        private void SetSpeechRateNative(float rate)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            androidBridge?.Call("setSpeechRate", rate);
#elif UNITY_IOS && !UNITY_EDITOR
            LovieTTS_SetSpeechRate(rate);
#endif
        }

        private void ShutdownNative()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass bridgeClass = new AndroidJavaClass("com.lovie.tts.TTSBridge"))
                {
                    bridgeClass.CallStatic("shutdown");
                }
            }
            catch (Exception)
            {
                // Engine already gone during app teardown — nothing to clean up.
            }
#elif UNITY_IOS && !UNITY_EDITOR
            LovieTTS_Shutdown();
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void LovieTTS_Init();
        [DllImport("__Internal")] private static extern bool LovieTTS_IsReady();
        [DllImport("__Internal")] private static extern bool LovieTTS_IsSpeaking();
        [DllImport("__Internal")] private static extern int LovieTTS_SetLanguage(string languageTag);
        [DllImport("__Internal")] private static extern void LovieTTS_SetSpeechRate(float rate);
        [DllImport("__Internal")] private static extern void LovieTTS_Speak(string text);
        [DllImport("__Internal")] private static extern void LovieTTS_Stop();
        [DllImport("__Internal")] private static extern void LovieTTS_Shutdown();
#endif
    }
}
