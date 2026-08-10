using System.Collections.Generic;

/// <summary>
/// Abstraction over where Word Wizard content comes from. Game code
/// depends only on this interface, never on a concrete source, so the
/// implementation can be swapped (JSON today, Firestore later) without
/// touching any game logic. Shape matches IPrefixSuffixContentRepository.
/// </summary>
public interface IWordWizardContentRepository
{
    void Initialize();
    List<WordWizardEntry> GetNextBatch(int count);
    bool HasMoreEntries { get; }

    /// <summary>Total number of entries available in the pool, set after Initialize().</summary>
    int TotalEntryCount { get; }
}
