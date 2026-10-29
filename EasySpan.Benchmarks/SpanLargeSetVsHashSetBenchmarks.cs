using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace EasySpan.Benchmarks;

/// <summary>
/// Compares <see cref="SpanLargeSet{T}"/> against <see cref="HashSet{T}"/> in end-to-end scenarios,
/// with <see cref="SpanSet{T}"/> included as a reference for the linear-search baseline.
/// </summary>
/// <remarks>
/// Both hash-based sides reuse storage allocated once in <see cref="Setup"/>: the span set is constructed over
/// pooled arrays (which clears its index table) and the hash set is presized and cleared, so the timed region
/// measures fill-and-use work rather than allocation. <see cref="SpanSet{T}"/> rows are omitted at the largest
/// size, where their O(n²) cost would dominate the run time without adding information.
/// </remarks>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class SpanLargeSetVsHashSetBenchmarks
{
    private int[] _items = null!;
    private int[] _table = null!;
    private int[] _linear = null!;
    private int[] _other = null!;
    private HashSet<int> _hashSet = null!;

    /// <summary>Number of elements used by each scenario.</summary>
    [Params(16, 256, 4096, 65536)]
    public int N { get; set; }

    /// <summary>Allocates reusable storage and an input array that shares only its last element with the benchmark items.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _items = new int[N];
        _table = new int[SpanLargeSet.RequiredTableLength(N)];
        _linear = new int[N];
        _hashSet = new HashSet<int>(N);

        _other = new int[N];
        for (var i = 0; i < N; i++) _other[i] = N + i;
        _other[N - 1] = N - 1;
    }

    private bool IncludeLinear => N <= 4096;

    #region Add

    /// <summary>Fills a SpanLargeSet with unique items.</summary>
    [Benchmark]
    [BenchmarkCategory("Add")]
    public int SpanLargeSet_Add()
    {
        var set = new SpanLargeSet<int>(_items, _table);
        for (var i = 0; i < N; i++) set.Add(i);
        return set.Count;
    }

    /// <summary>Clears and fills a presized HashSet with unique items.</summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Add")]
    public int HashSet_Add()
    {
        _hashSet.Clear();
        for (var i = 0; i < N; i++) _hashSet.Add(i);
        return _hashSet.Count;
    }

    /// <summary>Allocates a fresh presized HashSet and fills it with unique items.</summary>
    [Benchmark]
    [BenchmarkCategory("Add")]
    public int HashSet_Add_Allocating()
    {
        var set = new HashSet<int>(N);
        for (var i = 0; i < N; i++) set.Add(i);
        return set.Count;
    }

    /// <summary>Fills a SpanSet with unique items (linear search reference).</summary>
    [Benchmark]
    [BenchmarkCategory("Add")]
    public int SpanSet_Add()
    {
        if (!IncludeLinear) return 0;
        var set = new SpanSet<int>(_linear);
        for (var i = 0; i < N; i++) set.Add(i);
        return set.Count;
    }

    #endregion

    #region AddDuplicates

    /// <summary>Adds N items of which every second one is a duplicate to a SpanLargeSet.</summary>
    [Benchmark]
    [BenchmarkCategory("AddDuplicates")]
    public int SpanLargeSet_AddDuplicates()
    {
        var set = new SpanLargeSet<int>(_items, _table);
        for (var i = 0; i < N; i++) set.Add(i / 2);
        return set.Count;
    }

    /// <summary>Adds N items of which every second one is a duplicate to a HashSet.</summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("AddDuplicates")]
    public int HashSet_AddDuplicates()
    {
        _hashSet.Clear();
        for (var i = 0; i < N; i++) _hashSet.Add(i / 2);
        return _hashSet.Count;
    }

    /// <summary>Adds N items of which every second one is a duplicate to a SpanSet (linear search reference).</summary>
    [Benchmark]
    [BenchmarkCategory("AddDuplicates")]
    public int SpanSet_AddDuplicates()
    {
        if (!IncludeLinear) return 0;
        var set = new SpanSet<int>(_linear);
        for (var i = 0; i < N; i++) set.Add(i / 2);
        return set.Count;
    }

    #endregion

    #region Contains

    /// <summary>Fills a SpanLargeSet then searches for the last element.</summary>
    [Benchmark]
    [BenchmarkCategory("Contains_Hit")]
    public bool SpanLargeSet_Contains_Hit()
    {
        var set = new SpanLargeSet<int>(_items, _table);
        for (var i = 0; i < N; i++) set.Add(i);
        return set.Contains(N - 1);
    }

    /// <summary>Fills a HashSet then searches for the last element.</summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Contains_Hit")]
    public bool HashSet_Contains_Hit()
    {
        _hashSet.Clear();
        for (var i = 0; i < N; i++) _hashSet.Add(i);
        return _hashSet.Contains(N - 1);
    }

    /// <summary>Fills a SpanLargeSet then searches for an absent element.</summary>
    [Benchmark]
    [BenchmarkCategory("Contains_Miss")]
    public bool SpanLargeSet_Contains_Miss()
    {
        var set = new SpanLargeSet<int>(_items, _table);
        for (var i = 0; i < N; i++) set.Add(i);
        return set.Contains(-1);
    }

    /// <summary>Fills a HashSet then searches for an absent element.</summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Contains_Miss")]
    public bool HashSet_Contains_Miss()
    {
        _hashSet.Clear();
        for (var i = 0; i < N; i++) _hashSet.Add(i);
        return _hashSet.Contains(-1);
    }

    #endregion

    #region Enumerate

    /// <summary>Fills a SpanLargeSet then enumerates and sums its elements.</summary>
    [Benchmark]
    [BenchmarkCategory("EnumerateSum")]
    public int SpanLargeSet_EnumerateSum()
    {
        var set = new SpanLargeSet<int>(_items, _table);
        for (var i = 0; i < N; i++) set.Add(i);

        var sum = 0;
        foreach (var item in set) sum += item;
        return sum;
    }

    /// <summary>Fills a HashSet then enumerates and sums its elements.</summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("EnumerateSum")]
    public int HashSet_EnumerateSum()
    {
        _hashSet.Clear();
        for (var i = 0; i < N; i++) _hashSet.Add(i);

        var sum = 0;
        foreach (var item in _hashSet) sum += item;
        return sum;
    }

    #endregion

    #region Remove

    /// <summary>Fills a SpanLargeSet then removes every element.</summary>
    [Benchmark]
    [BenchmarkCategory("Remove")]
    public int SpanLargeSet_Remove()
    {
        var set = new SpanLargeSet<int>(_items, _table);
        for (var i = 0; i < N; i++) set.Add(i);
        for (var i = 0; i < N; i++) set.Remove(i);
        return set.Count;
    }

    /// <summary>Fills a HashSet then removes every element.</summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Remove")]
    public int HashSet_Remove()
    {
        _hashSet.Clear();
        for (var i = 0; i < N; i++) _hashSet.Add(i);
        for (var i = 0; i < N; i++) _hashSet.Remove(i);
        return _hashSet.Count;
    }

    /// <summary>Fills a SpanSet then removes every element (linear search reference).</summary>
    [Benchmark]
    [BenchmarkCategory("Remove")]
    public int SpanSet_Remove()
    {
        if (!IncludeLinear) return 0;
        var set = new SpanSet<int>(_linear);
        for (var i = 0; i < N; i++) set.Add(i);
        for (var i = 0; i < N; i++) set.Remove(i);
        return set.Count;
    }

    #endregion

    #region ClearRefill

    /// <summary>Fills, clears and refills a SpanLargeSet.</summary>
    [Benchmark]
    [BenchmarkCategory("ClearRefill")]
    public int SpanLargeSet_ClearRefill()
    {
        var set = new SpanLargeSet<int>(_items, _table);
        for (var i = 0; i < N; i++) set.Add(i);

        set.Clear();
        for (var i = 0; i < N; i++) set.Add(i);
        return set.Count;
    }

    /// <summary>Fills, clears and refills a HashSet.</summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ClearRefill")]
    public int HashSet_ClearRefill()
    {
        _hashSet.Clear();
        for (var i = 0; i < N; i++) _hashSet.Add(i);

        _hashSet.Clear();
        for (var i = 0; i < N; i++) _hashSet.Add(i);
        return _hashSet.Count;
    }

    #endregion

    #region Overlaps

    /// <summary>Fills a SpanLargeSet then checks overlap with a span whose only shared element is the last one.</summary>
    [Benchmark]
    [BenchmarkCategory("Overlaps")]
    public bool SpanLargeSet_Overlaps()
    {
        var set = new SpanLargeSet<int>(_items, _table);
        for (var i = 0; i < N; i++) set.Add(i);
        return set.Overlaps(_other);
    }

    /// <summary>Fills a HashSet then checks overlap with an array whose only shared element is the last one.</summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Overlaps")]
    public bool HashSet_Overlaps()
    {
        _hashSet.Clear();
        for (var i = 0; i < N; i++) _hashSet.Add(i);
        return _hashSet.Overlaps(_other);
    }

    /// <summary>Fills a SpanSet then checks overlap with a span whose only shared element is the last one (linear reference).</summary>
    [Benchmark]
    [BenchmarkCategory("Overlaps")]
    public bool SpanSet_Overlaps()
    {
        if (!IncludeLinear) return false;
        var set = new SpanSet<int>(_linear);
        for (var i = 0; i < N; i++) set.Add(i);
        return set.Overlaps(_other);
    }

    #endregion

    #region UnionWith

    /// <summary>Fills a half-capacity SpanLargeSet then unions it with a disjoint span of new elements.</summary>
    [Benchmark]
    [BenchmarkCategory("UnionWith")]
    public int SpanLargeSet_UnionWith()
    {
        var set = new SpanLargeSet<int>(_items, _table);
        for (var i = 0; i < N / 2; i++) set.Add(i);
        set.UnionWith(_other.AsSpan(0, N / 2));
        return set.Count;
    }

    /// <summary>Fills a half-size HashSet then unions it with a disjoint array of new elements.</summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("UnionWith")]
    public int HashSet_UnionWith()
    {
        _hashSet.Clear();
        for (var i = 0; i < N / 2; i++) _hashSet.Add(i);
        _hashSet.UnionWith(new ArraySegment<int>(_other, 0, N / 2));
        return _hashSet.Count;
    }

    #endregion
}
