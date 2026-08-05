using System;
using UnityEngine;

/// <summary>
/// Which side of the root word the correct affix option attaches to.
/// No project-wide Enums.cs exists in Shared/ (checked before adding this),
/// so this enum is defined here alongside the entry it describes.
/// </summary>
public enum PrefixSuffixMode
{
    Prefix,
    Suffix
}

/// <summary>
/// Plain data model representing a single prefix/suffix question: a root
/// word, which side of it is being tested, the multiple-choice affix
/// options, and which option is correct.
/// Instances are populated by deserializing repository content (local JSON
/// for now, backend later) — this is intentionally not a ScriptableObject so
/// swapping the content source never requires changing this class or
/// rebaking the build.
/// </summary>
[Serializable]
public class PrefixSuffixEntry
{
    #region Fields

    [SerializeField] private string id;
    [SerializeField] private string rootWord;
    [SerializeField] private PrefixSuffixMode mode;
    [SerializeField] private string[] options;
    [SerializeField] private int correctOptionIndex;

    #endregion

    #region Properties

    /// <summary>Unique identifier for this entry.</summary>
    public string Id => id;

    /// <summary>The root word the player is attaching a prefix or suffix to.</summary>
    public string RootWord => rootWord;

    /// <summary>Whether the correct option attaches as a prefix or a suffix.</summary>
    public PrefixSuffixMode Mode => mode;

    /// <summary>The multiple-choice affix options shown to the player.</summary>
    public string[] Options => options;

    /// <summary>Index into <see cref="Options"/> of the correct answer.</summary>
    public int CorrectOptionIndex => correctOptionIndex;

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
    /// <param name="rootWord">The root word the affix attaches to.</param>
    /// <param name="mode">Whether the correct option is a prefix or a suffix.</param>
    /// <param name="options">The multiple-choice affix options to display.</param>
    /// <param name="correctOptionIndex">Index into <paramref name="options"/> of the correct answer.</param>
    public PrefixSuffixEntry(string id, string rootWord, PrefixSuffixMode mode, string[] options, int correctOptionIndex)
    {
        this.id = id;
        this.rootWord = rootWord;
        this.mode = mode;
        this.options = options;
        this.correctOptionIndex = correctOptionIndex;
    }

    #endregion

    #region Private Methods

    // None.

    #endregion

    #region Events / Callbacks

    // None.

    #endregion
}
