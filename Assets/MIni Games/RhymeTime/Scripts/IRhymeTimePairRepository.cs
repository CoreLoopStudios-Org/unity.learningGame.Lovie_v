using System.Collections.Generic;

public interface IRhymeTimePairRepository
{
    void Initialize();
    List<RhymeTimeEntry> GetNextBatch(int pairCount);
    bool HasMorePairs { get; }

    /// <summary>Total number of pairs available in the pool, set after Initialize().</summary>
    int TotalPairCount { get; }
}