#if UNITY_EDITOR_WIN
using System.Diagnostics;

namespace Audio
{
    /// <summary>
    /// Lets you hear <see cref="GameVoice"/> inside the Windows Unity Editor, where the phone
    /// speech engines do not exist. Uses Windows' built-in voice through PowerShell. Editor only;
    /// it is not part of any build.
    /// </summary>
    internal static class EditorSpeech
    {
        #region Fields

        private static Process _current;

        #endregion

        #region Public Methods

        /// <summary>Speaks the text with the Windows voice, stopping any previous phrase.</summary>
        /// <param name="text">The text to read aloud.</param>
        internal static void Speak(string text)
        {
            Stop();

            string safeText = text.Replace("'", "''").Replace("\r", " ").Replace("\n", " ");
            string command = "Add-Type -AssemblyName System.Speech; " +
                             "(New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak('" + safeText + "')";

            var info = new ProcessStartInfo("powershell", "-NoProfile -Command \"" + command.Replace("\"", "\\\"") + "\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            _current = Process.Start(info);
        }

        #endregion

        #region Private Methods

        private static void Stop()
        {
            if (_current != null && !_current.HasExited)
            {
                _current.Kill();
            }

            _current = null;
        }

        #endregion
    }
}
#endif
