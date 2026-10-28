using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace EasySpan.Benchmarks;

/// <summary>
/// Measures <see cref="SpanSet{T}"/> query operations against <see cref="HashSet{T}"/> using
/// batches of queries over a pre-filled set, so that construction cost is amortized rather than measured.
/// </summary>
/// <remarks>
/// <see cref="SpanSet{T}"/> is a ref struct and cannot be stored in a field, so each span benchmark wraps
/// the pre-filled array with <see cref="SpanSet.CreateUnchecked{T}"/>, which is O(1), and then runs
/// <see cref="ContainsBatchSize"/> or <see cref="OverlapsBatchSize"/> queries. The hash set is pre-filled
/// in <see cref="Setup"/> and runs the identical query sequence.
/// </remarks>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class SpanSetQueryBenchmarks
{
    /// <summary>Number of membership queries executed per invocation.</summary>
    public const int ContainsBatchSize = 65536;

    /// <summary>Number of overlap checks executed per invocation.</summary>
    public const int OverlapsBatchSize = 64;

    /// <summary>Length of the short probe span used by the ShortOther overlap benchmarks.</summary>
    public const int ShortOtherLength = 8;

    private int[] _buffer = null!;
    private int[] _hitQueries = null!;
    private int[] _missQueries = null!;
    private int[] _other = null!;
    private int[] _shortOther = null!;
    private HashSet<int> _hashSet = null!;

    /// <summary>Number of elements held by each set.</summary>
    [Params(16, 32, 64, 256, 4096)]
    public int N { get; set; }

    /// <summary>Fills the shared storage, generates fixed query sequences, and prepares the overlap input.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _buffer = new int[N];
        for (var i = 0; i < N; i++) _buffer[i] = i;
        _hashSet = new HashSet<int>(_buffer);

        var random = new Random(12345);
        _hitQueries = new int[ContainsBatchSize];
        _missQueries = new int[ContainsBatchSize];
        for (var i = 0; i < ContainsBatchSize; i++)
        {
            _hitQueries[i] = random.Next(0, N);
            _missQueries[i] = random.Next(N, 2 * N);
        }

        _other = new int[N];
        for (var i = 0; i < N; i++) _other[i] = N + i;
        _other[N - 1] = N - 1;

        _shortOther = new int[ShortOtherLength];
        for (var i = 0; i < ShortOtherLength; i++) _shortOther[i] = N + i;
        _shortOther[ShortOtherLength - 1] = N - 1;
    }

    /// <summary>Runs present-element queries against a SpanSet constructed once per invocation.</summary>
    [Benchmark(Baseline = true, OperationsPerInvoke = ContainsBatchSize)]
    [BenchmarkCategory("Contains_Hit")]
    public int SpanSet_Contains_Hit()
    {
        var set = SpanSet.CreateUnchecked<int>(_buffer, N);
        return CountHits(set, _hitQueries);
    }

    /// <summary>Runs present-element queries against a pre-filled HashSet.</summary>
    [Benchmark(OperationsPerInvoke = ContainsBatchSize)]
    [BenchmarkCategory("Contains_Hit")]
    public int HashSet_Contains_Hit() => CountHits(_hashSet, _hitQueries);

    /// <summary>Runs absent-element queries against a SpanSet constructed once per invocation.</summary>
    [Benchmark(Baseline = true, OperationsPerInvoke = ContainsBatchSize)]
    [BenchmarkCategory("Contains_Miss")]
    public int SpanSet_Contains_Miss()
    {
        var set = SpanSet.CreateUnchecked<int>(_buffer, N);
        return CountHits(set, _missQueries);
    }

    /// <summary>Runs absent-element queries against a pre-filled HashSet.</summary>
    [Benchmark(OperationsPerInvoke = ContainsBatchSize)]
    [BenchmarkCategory("Contains_Miss")]
    public int HashSet_Contains_Miss() => CountHits(_hashSet, _missQueries);

    /// <summary>Repeats an overlap check whose only shared element is the last one against a SpanSet.</summary>
    [Benchmark(Baseline = true, OperationsPerInvoke = OverlapsBatchSize)]
    [BenchmarkCategory("Overlaps")]
    public int SpanSet_Overlaps()
    {
        var set = SpanSet.CreateUnchecked<int>(_buffer, N);
        var hits = 0;
        for (var i = 0; i < OverlapsBatchSize; i++)
        {
            if (set.Overlaps(_other)) hits++;
        }

        return hits;
    }

    /// <summary>Repeats an overlap check whose only shared element is the last one against a HashSet.</summary>
    [Benchmark(OperationsPerInvoke = OverlapsBatchSize)]
    [BenchmarkCategory("Overlaps")]
    public int HashSet_Overlaps()
    {
        var hits = 0;
        for (var i = 0; i < OverlapsBatchSize; i++)
        {
            if (_hashSet.Overlaps(_other)) hits++;
        }

        return hits;
    }

    /// <summary>Repeats an overlap check with a short probe span whose last element is shared, against a SpanSet.</summary>
    [Benchmark(Baseline = true, OperationsPerInvoke = OverlapsBatchSize)]
    [BenchmarkCategory("Overlaps_ShortOther")]
    public int SpanSet_Overlaps_ShortOther()
    {
        var set = SpanSet.CreateUnchecked<int>(_buffer, N);
        var hits = 0;
        for (var i = 0; i < OverlapsBatchSize; i++)
        {
            if (set.Overlaps(_shortOther)) hits++;
        }

        return hits;
    }

    /// <summary>Repeats an overlap check with a short probe span whose last element is shared, against a HashSet.</summary>
    [Benchmark(OperationsPerInvoke = OverlapsBatchSize)]
    [BenchmarkCategory("Overlaps_ShortOther")]
    public int HashSet_Overlaps_ShortOther()
    {
        var hits = 0;
        for (var i = 0; i < OverlapsBatchSize; i++)
        {
            if (_hashSet.Overlaps(_shortOther)) hits++;
        }

        return hits;
    }

    private static int CountHits(SpanSet<int> set, int[] queries)
    {
        var hits = 0;
        for (var i = 0; i < queries.Length; i++)
        {
            if (set.Contains(queries[i])) hits++;
        }

        return hits;
    }

    private static int CountHits(HashSet<int> set, int[] queries)
    {
        var hits = 0;
        for (var i = 0; i < queries.Length; i++)
        {
            if (set.Contains(queries[i])) hits++;
        }

        return hits;
    }
}
