namespace Audio
{
    /// <summary>
    /// One-line access to the device text-to-speech for mini-games. Games call this when a
    /// word has no recorded audio, so every word can still be heard. Silent in the Editor.
    /// </summary>
    public static class GameVoice
    {
        #region Public Methods

        /// <summary>Speaks the given word or sentence, interrupting any current speech.</summary>
        /// <param name="text">The text to read aloud. Empty text is ignored.</param>
        public static void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

#if UNITY_EDITOR_WIN
            EditorSpeech.Speak(text);
            return;
#else
            MobileTTS tts = MobileTTS.Instance;
            if (tts != null)
            {
                tts.Speak(text);
            }
#endif
        }

        #endregion
    }
}
