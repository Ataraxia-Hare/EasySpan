using BenchmarkDotNet.Attributes;

namespace EasySpan.Benchmarks;

/// <summary>
/// Compares <see cref="SpanSet{T}"/> against <see cref="HashSet{T}"/> in representative scenarios.
/// </summary>
[MemoryDiagnoser]
public class SpanSetVsHashSetBenchmarks
{
    private int[] _other = null!;
    private HashSet<int> _hashSet = null!;

    /// <summary>Number of elements used by each scenario.</summary>
    [Params(16, 256, 4096)]
    public int N { get; set; }

    /// <summary>Prepares an empty hash set and an input array that shares only its last element with the benchmark items.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _hashSet = new HashSet<int>(N);

        _other = new int[N];
        for (var i = 0; i < N; i++) _other[i] = N + i;
        _other[N - 1] = N - 1;
    }

    /// <summary>Fills a SpanSet over a stack-allocated buffer with unique items.</summary>
    [Benchmark]
    public int SpanSet_Add()
    {
        Span<int> buffer = stackalloc int[N];
        var set = new SpanSet<int>(buffer);
        for (var i = 0; i < N; i++) set.Add(i);
        return set.Count;
    }

    /// <summary>Fills a HashSet created with default capacity with unique items.</summary>
    [Benchmark(Baseline = true)]
    public int HashSet_Add()
    {
        var set = new HashSet<int>();
        for (var i = 0; i < N; i++) set.Add(i);
        return set.Count;
    }

    /// <summary>Fills a HashSet created with preset capacity with unique items.</summary>
    [Benchmark]
    public int HashSet_Add_Presized()
    {
        var set = new HashSet<int>(N);
        for (var i = 0; i < N; i++) set.Add(i);
        return set.Count;
    }

    /// <summary>Adds N items of which every second one is a duplicate to a SpanSet.</summary>
    [Benchmark]
    public int SpanSet_AddDuplicates()
    {
        Span<int> buffer = stackalloc int[N];
        var set = new SpanSet<int>(buffer);
        for (var i = 0; i < N; i++) set.Add(i / 2);
        return set.Count;
    }

    /// <summary>Adds N items of which every second one is a duplicate to a HashSet.</summary>
    [Benchmark]
    public int HashSet_AddDuplicates()
    {
        var set = new HashSet<int>(N);
        for (var i = 0; i < N; i++) set.Add(i / 2);
        return set.Count;
    }

    /// <summary>Fills a SpanSet then searches for the last element.</summary>
    [Benchmark]
    public bool SpanSet_Contains_Hit()
    {
        Span<int> buffer = stackalloc int[N];
        var set = new SpanSet<int>(buffer);
        for (var i = 0; i < N; i++) set.Add(i);
        return set.Contains(N - 1);
    }

    /// <summary>Fills a HashSet then searches for the last element.</summary>
    [Benchmark]
    public bool HashSet_Contains_Hit()
    {
        _hashSet.Clear();
        for (var i = 0; i < N; i++) _hashSet.Add(i);
        return _hashSet.Contains(N - 1);
    }

    /// <summary>Fills a SpanSet then searches for an absent element.</summary>
    [Benchmark]
    public bool SpanSet_Contains_Miss()
    {
        Span<int> buffer = stackalloc int[N];
        var set = new SpanSet<int>(buffer);
        for (var i = 0; i < N; i++) set.Add(i);
        return set.Contains(-1);
    }

    /// <summary>Fills a HashSet then searches for an absent element.</summary>
    [Benchmark]
    public bool HashSet_Contains_Miss()
    {
        _hashSet.Clear();
        for (var i = 0; i < N; i++) _hashSet.Add(i);
        return _hashSet.Contains(-1);
    }

    /// <summary>Fills a SpanSet then enumerates and sums its elements.</summary>
    [Benchmark]
    public int SpanSet_EnumerateSum()
    {
        Span<int> buffer = stackalloc int[N];
        var set = new SpanSet<int>(buffer);
        for (var i = 0; i < N; i++) set.Add(i);

        var sum = 0;
        foreach (var item in set) sum += item;
        return sum;
    }

    /// <summary>Fills a HashSet then enumerates and sums its elements.</summary>
    [Benchmark]
    public int HashSet_EnumerateSum()
    {
        _hashSet.Clear();
        for (var i = 0; i < N; i++) _hashSet.Add(i);

        var sum = 0;
        foreach (var item in _hashSet) sum += item;
        return sum;
    }

    /// <summary>Fills a SpanSet then removes the first added element.</summary>
    [Benchmark]
    public int SpanSet_Remove()
    {
        Span<int> buffer = stackalloc int[N];
        var set = new SpanSet<int>(buffer);
        for (var i = 0; i < N; i++) set.Add(i);
        set.Remove(0);
        return set.Count;
    }

    /// <summary>Fills a HashSet then removes the first added element.</summary>
    [Benchmark]
    public int HashSet_Remove()
    {
        var set = new HashSet<int>(N);
        for (var i = 0; i < N; i++) set.Add(i);

        set.Remove(0);
        return set.Count;
    }

    /// <summary>Fills, clears and refills a SpanSet.</summary>
    [Benchmark]
    public int SpanSet_ClearRefill()
    {
        Span<int> buffer = stackalloc int[N];
        var set = new SpanSet<int>(buffer);
        for (var i = 0; i < N; i++) set.Add(i);

        set.Clear();
        for (var i = 0; i < N; i++) set.Add(i);
        return set.Count;
    }

    /// <summary>Fills, clears and refills a HashSet.</summary>
    [Benchmark]
    public int HashSet_ClearRefill()
    {
        var set = new HashSet<int>(N);
        for (var i = 0; i < N; i++) set.Add(i);

        set.Clear();
        for (var i = 0; i < N; i++) set.Add(i);
        return set.Count;
    }

    /// <summary>Fills a SpanSet then checks overlap with a span whose only shared element is the last one.</summary>
    [Benchmark]
    public bool SpanSet_Overlaps()
    {
        Span<int> buffer = stackalloc int[N];
        var set = new SpanSet<int>(buffer);
        for (var i = 0; i < N; i++) set.Add(i);
        return set.Overlaps(_other);
    }

    /// <summary>Fills a HashSet then checks overlap with an array whose only shared element is the last one.</summary>
    [Benchmark]
    public bool HashSet_Overlaps()
    {
        _hashSet.Clear();
        for (var i = 0; i < N; i++) _hashSet.Add(i);
        return _hashSet.Overlaps(_other);
    }

    /// <summary>Fills a half-capacity SpanSet then unions it with a disjoint span of new elements.</summary>
    [Benchmark]
    public int SpanSet_UnionWith()
    {
        Span<int> buffer = stackalloc int[N];
        var set = new SpanSet<int>(buffer);
        for (var i = 0; i < N / 2; i++) set.Add(i);
        set.UnionWith(_other.AsSpan(0, N / 2));
        return set.Count;
    }

    /// <summary>Fills a half-size HashSet then unions it with a disjoint array of new elements.</summary>
    [Benchmark]
    public int HashSet_UnionWith()
    {
        var set = new HashSet<int>(N);
        for (var i = 0; i < N / 2; i++) set.Add(i);
        set.UnionWith(new ArraySegment<int>(_other, 0, N / 2));
        return set.Count;
    }
}
