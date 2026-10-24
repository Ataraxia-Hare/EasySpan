using BenchmarkDotNet.Attributes;

namespace EasySpan.Benchmarks;

/// <summary>
/// Compares <see cref="SpanList{T}"/> against <see cref="List{T}"/> in representative scenarios.
/// </summary>
[MemoryDiagnoser]
public class SpanListVsListBenchmarks
{
    private int[] _buffer = null!;
    private List<int> _list = null!;

    /// <summary>Number of elements used by each scenario.</summary>
    [Params(16, 256, 4096)]
    public int N { get; set; }

    /// <summary>Prepares a buffer and a filled list.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _buffer = new int[N];
        _list = new List<int>(N);
        for (var i = 0; i < N; i++) _list.Add(i);
    }

    private SpanList<int> Filled()
    {
        var list = new SpanList<int>(_buffer);
        for (var i = 0; i < N; i++) list.Add(i);
        return list;
    }

    /// <summary>Fills a SpanList over an existing buffer.</summary>
    [Benchmark]
    public int SpanList_Add()
    {
        var list = new SpanList<int>(_buffer);
        for (var i = 0; i < N; i++) list.Add(i);
        return list.Count;
    }

    /// <summary>Fills a List created with default capacity.</summary>
    [Benchmark(Baseline = true)]
    public int List_Add()
    {
        var list = new List<int>();
        for (var i = 0; i < N; i++) list.Add(i);
        return list.Count;
    }

    /// <summary>Fills a List created with preset capacity.</summary>
    [Benchmark]
    public int List_Add_Presized()
    {
        var list = new List<int>(N);
        for (var i = 0; i < N; i++) list.Add(i);
        return list.Count;
    }

    /// <summary>Fills a SpanList then sums via the indexer.</summary>
    [Benchmark]
    public int SpanList_IndexerSum()
    {
        var list = Filled();
        var sum = 0;
        for (var i = 0; i < list.Count; i++) sum += list[i];
        return sum;
    }

    /// <summary>Sums elements via the List indexer.</summary>
    [Benchmark]
    public int List_IndexerSum()
    {
        var sum = 0;
        for (var i = 0; i < _list.Count; i++) sum += _list[i];
        return sum;
    }

    /// <summary>Fills a SpanList then searches for the last element.</summary>
    [Benchmark]
    public bool SpanList_Contains() => Filled().Contains(N - 1);

    /// <summary>Searches for the last element in a List.</summary>
    [Benchmark]
    public bool List_Contains() => _list.Contains(N - 1);

    /// <summary>Fills a SpanList then finds the last element's index.</summary>
    [Benchmark]
    public int SpanList_IndexOf() => Filled().IndexOf(N - 1);

    /// <summary>Finds the index of the last element in a List.</summary>
    [Benchmark]
    public int List_IndexOf() => _list.IndexOf(N - 1);

    /// <summary>Fills a SpanList then removes the first element (worst-case shift).</summary>
    [Benchmark]
    public int SpanList_RemoveAt()
    {
        var list = Filled();
        list.RemoveAt(0);
        return list.Count;
    }

    /// <summary>Copies a List then removes the first element (worst-case shift).</summary>
    [Benchmark]
    public int List_RemoveAt()
    {
        var list = new List<int>(_list);
        list.RemoveAt(0);
        return list.Count;
    }

    /// <summary>Fills, clears and refills a SpanList.</summary>
    [Benchmark]
    public int SpanList_ClearRefill()
    {
        var list = Filled();
        list.Clear();
        for (var i = 0; i < N; i++) list.Add(i);
        return list.Count;
    }

    /// <summary>Copies, clears and refills a List.</summary>
    [Benchmark]
    public int List_ClearRefill()
    {
        var list = new List<int>(_list);
        list.Clear();
        for (var i = 0; i < N; i++) list.Add(i);
        return list.Count;
    }
}
