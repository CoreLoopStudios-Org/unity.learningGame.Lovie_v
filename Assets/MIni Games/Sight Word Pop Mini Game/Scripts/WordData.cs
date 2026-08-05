using UnityEngine;

/// <summary>
/// Represents a single word entry with its audio clip.
/// Future: swap AudioClip for a URL string when backend is ready.
/// </summary>
[System.Serializable]
public class WordEntry
{
    public string word;
    public AudioClip audioClip; // Pre-recorded clip. Replace with URL for backend.
}