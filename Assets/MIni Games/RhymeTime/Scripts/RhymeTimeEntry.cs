using System;
using UnityEngine;

/// <summary>
/// Plain data model representing a single word participating in a rhyme-matching pair.
/// Two entries that rhyme together share the same <see cref="PairId"/> — correctness is
/// determined by comparing that key, not by object reference (unlike WordMatch, where both
/// sides of a pair share one entry instance).
/// Instances are populated by deserializing repository content (local JSON for now, backend
/// later) — this is intentionally not a ScriptableObject so swapping the content source never
/// requires changing this class or rebaking the build.
/// </summary>
[Serializable]
public class RhymeTimeEntry
{
    #region Fields

    [SerializeField] private string id;
    [SerializeField] private string word;
    [SerializeField] private string pairId;

    #endregion

    #region Properties

    /// <summary>Unique identifier for this specific word entry.</summary>
    public string Id => id;

    /// <summary>The word displayed on the card.</summary>
    public string Word => word;

    /// <summary>
    /// Shared key linking two entries together as a correct rhyming pair.
    /// Two entries form a correct match when their <see cref="PairId"/> values are equal.
    /// </summary>
    public string PairId => pairId;

    #endregion

    #region Unity Lifecycle

    // None — plain data class, not a MonoBehaviour.

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a new entry. Used by the repository layer when constructing entries
    /// from deserialized content.
    /// </summary>
    /// <param name="id">Unique identifier for this entry.</param>
    /// <param name="word">The word to display on the card.</param>
    /// <param name="pairId">The shared key that links this entry to its rhyming partner.</param>
    public RhymeTimeEntry(string id, string word, string pairId)
    {
        this.id = id;
        this.word = word;
        this.pairId = pairId;
    }

    #endregion

    #region Private Methods

    // None.

    #endregion

    #region Events / Callbacks

    // None.

    #endregion
}