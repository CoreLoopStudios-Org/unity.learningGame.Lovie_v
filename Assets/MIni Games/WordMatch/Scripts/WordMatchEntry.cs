using UnityEngine;
using System;

namespace CoreLoop.WordMatch
{
    [Serializable]
    public class WordMatchEntry
    {
        public string id; // Unique identifier for matching
        public string word;
        public Sprite image;
        public AudioClip audioClip;
    }
}
