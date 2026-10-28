using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace EasySpan.Benchmarks;

/// <summary>
/// Measures individual <see cref="SpanList{T}"/> query operations against <see cref="List{T}"/>
/// over pre-filled storage, excluding the cost of building the collection.
/// </summary>
/// <remarks>
/// The backing array and the list are filled in <see cref="Setup"/>. Each span benchmark wraps the
/// pre-filled array with the count constructor, which is O(1) for <see cref="SpanList{T}"/>.
/// The batch benchmarks compare the same query sequence over a stack-allocated and a heap-allocated
/// buffer; the stack variant amortizes its allocation and fill over <see cref="BatchSize"/> queries.
/// </remarks>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class SpanListQueryBenchmarks
{
    /// <summary>Number of queries executed per invocation in the batch benchmarks.</summary>
    public const int BatchSize = 65536;

    private int[] _buffer = null!;
    private int[] _queries = null!;
    private List<int> _list = null!;
    private int _first;
    private int _last;
    private int _missing;

    /// <summary>Number of elements held by each collection.</summary>
    [Params(16, 32, 64, 256, 4096)]
    public int N { get; set; }

    /// <summary>Fills the shared storage and generates a fixed query sequence with roughly 50% hits.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _buffer = new int[N];
        for (var i = 0; i < N; i++) _buffer[i] = i;
        _list = new List<int>(_buffer);

        _first = 0;
        _last = N - 1;
        _missing = -1;

        var random = new Random(12345);
        _queries = new int[BatchSize];
        for (var i = 0; i < BatchSize; i++) _queries[i] = random.Next(0, 2 * N);
    }

    /// <summary>Searches for the first element in a pre-filled SpanList.</summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Contains_First")]
    public bool SpanList_Contains_First()
    {
        var list = new SpanList<int>(_buffer, N);
        return list.Contains(_first);
    }

    /// <summary>Searches for the first element in a pre-filled List.</summary>
    [Benchmark]
    [BenchmarkCategory("Contains_First")]
    public bool List_Contains_First() => _list.Contains(_first);

    /// <summary>Searches for the last element in a pre-filled SpanList.</summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Contains_Last")]
    public bool SpanList_Contains_Last()
    {
        var list = new SpanList<int>(_buffer, N);
        return list.Contains(_last);
    }

    /// <summary>Searches for the last element in a pre-filled List.</summary>
    [Benchmark]
    [BenchmarkCategory("Contains_Last")]
    public bool List_Contains_Last() => _list.Contains(_last);

    /// <summary>Searches for an absent element in a pre-filled SpanList.</summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Contains_Miss")]
    public bool SpanList_Contains_Miss()
    {
        var list = new SpanList<int>(_buffer, N);
        return list.Contains(_missing);
    }

    /// <summary>Searches for an absent element in a pre-filled List.</summary>
    [Benchmark]
    [BenchmarkCategory("Contains_Miss")]
    public bool List_Contains_Miss() => _list.Contains(_missing);

    /// <summary>Finds the index of the last element in a pre-filled SpanList.</summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("IndexOf_Last")]
    public int SpanList_IndexOf_Last()
    {
        var list = new SpanList<int>(_buffer, N);
        return list.IndexOf(_last);
    }

    /// <summary>Finds the index of the last element in a pre-filled List.</summary>
    [Benchmark]
    [BenchmarkCategory("IndexOf_Last")]
    public int List_IndexOf_Last() => _list.IndexOf(_last);

    /// <summary>Sums a pre-filled SpanList via the indexer.</summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("IndexerSum")]
    public int SpanList_IndexerSum()
    {
        var list = new SpanList<int>(_buffer, N);
        var sum = 0;
        for (var i = 0; i < list.Count; i++) sum += list[i];
        return sum;
    }

    /// <summary>Sums a pre-filled List via the indexer.</summary>
    [Benchmark]
    [BenchmarkCategory("IndexerSum")]
    public int List_IndexerSum()
    {
        var sum = 0;
        for (var i = 0; i < _list.Count; i++) sum += _list[i];
        return sum;
    }

    /// <summary>Runs the query batch against a SpanList over a pre-filled heap array.</summary>
    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    [BenchmarkCategory("ContainsBatch")]
    public int SpanList_ContainsBatch_HeapBuffer()
    {
        var list = new SpanList<int>(_buffer, N);
        var queries = _queries;
        var hits = 0;
        for (var i = 0; i < queries.Length; i++)
        {
            if (list.Contains(queries[i])) hits++;
        }

        return hits;
    }

    /// <summary>Runs the query batch against a SpanList over a stack buffer filled inside the benchmark.</summary>
    [Benchmark(OperationsPerInvoke = BatchSize)]
    [BenchmarkCategory("ContainsBatch")]
    public int SpanList_ContainsBatch_StackBuffer()
    {
        Span<int> buffer = stackalloc int[N];
        for (var i = 0; i < N; i++) buffer[i] = i;
        var list = new SpanList<int>(buffer, N);

        var queries = _queries;
        var hits = 0;
        for (var i = 0; i < queries.Length; i++)
        {
            if (list.Contains(queries[i])) hits++;
        }

        return hits;
    }

    /// <summary>Runs the query batch against a pre-filled List.</summary>
    [Benchmark(OperationsPerInvoke = BatchSize)]
    [BenchmarkCategory("ContainsBatch")]
    public int List_ContainsBatch()
    {
        var queries = _queries;
        var hits = 0;
        for (var i = 0; i < queries.Length; i++)
        {
            if (_list.Contains(queries[i])) hits++;
        }

        return hits;
    }
}
