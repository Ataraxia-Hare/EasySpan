using BenchmarkDotNet.Attributes;

namespace EasySpan.Benchmarks;

/// <summary>
/// Compares <see cref="SpanSet{T}"/> against <see cref="HashSet{T}"/> in representative scenarios.
/// </summary>
[MemoryDiagnoser]
public class SpanSetVsHashSetBenchmarks
{
    private int[] _buffer = null!;
    private int[] _other = null!;
    private HashSet<int> _hashSet = null!;

    /// <summary>Number of elements used by each scenario.</summary>
    [Params(16, 256, 4096)]
    public int N { get; set; }

    /// <summary>Prepares a buffer, a filled hash set and an input span that shares only its last element.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _buffer = new int[N];
        _hashSet = new HashSet<int>(N);
        for (var i = 0; i < N; i++) _hashSet.Add(i);

        _other = new int[N];
        for (var i = 0; i < N; i++) _other[i] = N + i;
        _other[N - 1] = N - 1;
    }

    private SpanSet<int> Filled()
    {
        var set = new SpanSet<int>(_buffer);
        for (var i = 0; i < N; i++) set.Add(i);
        return set;
    }

    /// <summary>Fills a SpanSet over an existing buffer with unique items.</summary>
    [Benchmark]
    public int SpanSet_Add()
    {
        var set = new SpanSet<int>(_buffer);
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
        var set = new SpanSet<int>(_buffer);
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
    public bool SpanSet_Contains_Hit() => Filled().Contains(N - 1);

    /// <summary>Searches for the last element in a HashSet.</summary>
    [Benchmark]
    public bool HashSet_Contains_Hit() => _hashSet.Contains(N - 1);

    /// <summary>Fills a SpanSet then searches for an absent element.</summary>
    [Benchmark]
    public bool SpanSet_Contains_Miss() => Filled().Contains(-1);

    /// <summary>Searches for an absent element in a HashSet.</summary>
    [Benchmark]
    public bool HashSet_Contains_Miss() => _hashSet.Contains(-1);

    /// <summary>Fills a SpanSet then enumerates and sums its elements.</summary>
    [Benchmark]
    public int SpanSet_EnumerateSum()
    {
        var set = Filled();
        var sum = 0;
        foreach (var item in set) sum += item;
        return sum;
    }

    /// <summary>Enumerates and sums the elements of a HashSet.</summary>
    [Benchmark]
    public int HashSet_EnumerateSum()
    {
        var sum = 0;
        foreach (var item in _hashSet) sum += item;
        return sum;
    }

    /// <summary>Fills a SpanSet then removes the first added element.</summary>
    [Benchmark]
    public int SpanSet_Remove()
    {
        var set = Filled();
        set.Remove(0);
        return set.Count;
    }

    /// <summary>Copies a HashSet then removes the first added element.</summary>
    [Benchmark]
    public int HashSet_Remove()
    {
        var set = new HashSet<int>(_hashSet);
        set.Remove(0);
        return set.Count;
    }

    /// <summary>Fills, clears and refills a SpanSet.</summary>
    [Benchmark]
    public int SpanSet_ClearRefill()
    {
        var set = Filled();
        set.Clear();
        for (var i = 0; i < N; i++) set.Add(i);
        return set.Count;
    }

    /// <summary>Copies, clears and refills a HashSet.</summary>
    [Benchmark]
    public int HashSet_ClearRefill()
    {
        var set = new HashSet<int>(_hashSet);
        set.Clear();
        for (var i = 0; i < N; i++) set.Add(i);
        return set.Count;
    }

    /// <summary>Fills a SpanSet then checks overlap with a span whose only shared element is the last one.</summary>
    [Benchmark]
    public bool SpanSet_Overlaps() => Filled().Overlaps(_other);

    /// <summary>Checks overlap between a HashSet and an array whose only shared element is the last one.</summary>
    [Benchmark]
    public bool HashSet_Overlaps() => _hashSet.Overlaps(_other);

    /// <summary>Fills a half-capacity SpanSet then unions it with a disjoint span of new elements.</summary>
    [Benchmark]
    public int SpanSet_UnionWith()
    {
        var set = new SpanSet<int>(_buffer);
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
