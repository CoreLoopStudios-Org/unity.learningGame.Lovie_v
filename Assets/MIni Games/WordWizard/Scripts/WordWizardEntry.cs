using System;
using UnityEngine;

/// <summary>
/// Plain data model representing a single word to spell: its target word
/// and optional decoy letters shown in the pool as distractors.
/// Instances are populated by deserializing repository content (local JSON
/// for now, backend later) — this is intentionally not a ScriptableObject so
/// swapping the content source never requires changing this class or
/// rebaking the build.
/// </summary>
[Serializable]
public class WordWizardEntry
{
    #region Fields

    [SerializeField] private string id;
    [SerializeField] private string targetWord;
    [SerializeField] private string decoyLetters;

    #endregion

    #region Properties

    /// <summary>Unique identifier for this entry.</summary>
    public string Id => id;

    /// <summary>The word the player needs to spell.</summary>
    public string TargetWord => targetWord;

    /// <summary>Optional extra letters added to the pool as distractors.</summary>
    public string DecoyLetters => decoyLetters;

    #endregion

    #region Unity Lifecycle

    // None — plain data class, not a MonoBehaviour.

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a new entry. Used by the repository layer when constructing
    /// entries from deserialized content.
    /// </summary>
    /// <param name="id">Unique identifier for this entry.</param>
    /// <param name="targetWord">The word the player needs to spell.</param>
    /// <param name="decoyLetters">Optional extra letters added to the pool as distractors.</param>
    public WordWizardEntry(string id, string targetWord, string decoyLetters)
    {
        this.id = id;
        this.targetWord = targetWord;
        this.decoyLetters = decoyLetters;
    }

    #endregion

    #region Private Methods

    // None.

    #endregion

    #region Events / Callbacks

    // None.

    #endregion
}
