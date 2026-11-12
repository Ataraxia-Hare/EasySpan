namespace EasySpan.Tests;

/// <summary>
/// Unit tests for <see cref="SpanLargeSet{T}"/>.
/// </summary>
public class SpanLargeSetTests
{
    #region Table Sizing / Validation

    /// <summary>
    /// <see cref="SpanLargeSet.RequiredTableLength"/> returns the smallest power of two that is at least twice the capacity.
    /// </summary>
    /// <param name="capacity">The item capacity.</param>
    /// <param name="expected">The expected table length.</param>
    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 8)]
    [InlineData(4, 8)]
    [InlineData(5, 16)]
    [InlineData(16, 32)]
    [InlineData(17, 64)]
    [InlineData(4096, 8192)]
    public void RequiredTableLength_ReturnsSmallestValidPowerOfTwo(int capacity, int expected)
    {
        Assert.Equal(expected, SpanLargeSet.RequiredTableLength(capacity));
    }

    /// <summary>
    /// A negative capacity is rejected.
    /// </summary>
    [Fact]
    public void RequiredTableLength_NegativeCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SpanLargeSet.RequiredTableLength(-1));
    }

    /// <summary>
    /// Table lengths that are too short, zero, or not a power of two are rejected.
    /// </summary>
    /// <param name="tableLength">The invalid table length.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(7)]
    public void Constructor_WithInvalidTableLength_Throws(int tableLength)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            _ = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[tableLength]);
        });

        Assert.Equal("table", ex.ParamName);
    }

    /// <summary>
    /// A table larger than required is accepted.
    /// </summary>
    [Fact]
    public void Constructor_WithOversizedTable_Succeeds()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[64]);
        for (var i = 0; i < 4; i++)
        {
            set.Add(i);
        }

        Assert.True(set.IsFull());
    }

    /// <summary>
    /// The constructor empties the table so stale index data cannot leak in.
    /// </summary>
    [Fact]
    public void Constructor_ClearsStaleTable()
    {
        Span<int> table = stackalloc int[8];
        table.Fill(3);

        var set = new SpanLargeSet<int>(stackalloc int[4], table);

        Assert.False(set.Contains(0));
        Assert.True(set.Add(0));
    }

    #endregion

    #region Constructors

    /// <summary>
    /// The two-span constructor creates an empty set whose capacity equals the item span length.
    /// </summary>
    [Fact]
    public void Constructor_WithSpans_CreatesEmptySet()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);

        Assert.Equal(0, set.Count);
        Assert.Equal(4, set.Capacity);
        Assert.False(set.IsFull());
    }

    /// <summary>
    /// A set over an empty item span has zero capacity and is both empty and full.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptySpan_IsFullAndEmpty()
    {
        var set = new SpanLargeSet<int>(Span<int>.Empty, stackalloc int[2]);

        Assert.Equal(0, set.Count);
        Assert.Equal(0, set.Capacity);
        Assert.True(set.IsFull());
        Assert.False(set.Contains(1));
    }

    /// <summary>
    /// The count constructor indexes the first <paramref name="count"/> existing unique elements.
    /// </summary>
    /// <param name="count">The initial element count.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public void Constructor_WithCount_SetsCountAndIndexesExistingData(int count)
    {
        var set = new SpanLargeSet<int>(stackalloc int[] { 1, 2, 3, 4 }, stackalloc int[8], count);

        Assert.Equal(count, set.Count);
        Assert.Equal(4, set.Capacity);
        for (var i = 1; i <= 4; i++)
        {
            Assert.Equal(i <= count, set.Contains(i));
        }
    }

    /// <summary>
    /// A count greater than the item span length throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [Fact]
    public void Constructor_WithCountGreaterThanCapacity_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new SpanLargeSet<int>(stackalloc int[2], stackalloc int[4], 3);
        });

        Assert.Equal("count", ex.ParamName);
    }

    /// <summary>
    /// Duplicate initial elements throw <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void Constructor_WithDuplicateInitialElements_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            _ = new SpanLargeSet<int>(stackalloc int[] { 1, 2, 1 }, stackalloc int[8], 3);
        });

        Assert.Equal("items", ex.ParamName);
    }

    /// <summary>
    /// Duplicates beyond <c>count</c> are ignored.
    /// </summary>
    [Fact]
    public void Constructor_WithDuplicatesBeyondCount_Succeeds()
    {
        var set = new SpanLargeSet<int>(stackalloc int[] { 1, 2, 1, 2 }, stackalloc int[8], 2);

        Assert.Equal(2, set.Count);
        Assert.False(set.Add(1));
    }

    /// <summary>
    /// <see cref="SpanLargeSet.CreateUnchecked{T}"/> indexes the leading elements without validation.
    /// </summary>
    [Fact]
    public void CreateUnchecked_SetsCountAndIndexesExistingData()
    {
        var set = SpanLargeSet.CreateUnchecked<int>(stackalloc int[] { 5, 6, 7, 8 }, stackalloc int[8], 3);

        Assert.Equal(3, set.Count);
        Assert.True(set.Contains(5));
        Assert.True(set.Contains(7));
        Assert.False(set.Contains(8));
    }

    /// <summary>
    /// <see cref="SpanLargeSet.CreateUnchecked{T}"/> still validates the count against the capacity.
    /// </summary>
    [Fact]
    public void CreateUnchecked_WithCountGreaterThanCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = SpanLargeSet.CreateUnchecked<int>(stackalloc int[2], stackalloc int[4], 3);
        });
    }

    /// <summary>
    /// <see cref="SpanLargeSet.CreateUnchecked{T}"/> does not inspect the elements for duplicates.
    /// </summary>
    [Fact]
    public void CreateUnchecked_WithDuplicateInitialElements_DoesNotThrow()
    {
        var set = SpanLargeSet.CreateUnchecked<int>(stackalloc int[] { 1, 1 }, stackalloc int[4], 2);

        Assert.Equal(2, set.Count);
    }

    #endregion

    #region Add / TryAdd

    /// <summary>
    /// Adding new items returns true and stores them contiguously in the item span.
    /// </summary>
    [Fact]
    public void Add_NewItems_ReturnsTrueAndWritesToBackingSpan()
    {
        Span<int> items = stackalloc int[4];
        var set = new SpanLargeSet<int>(items, stackalloc int[8]);

        Assert.True(set.Add(10));
        Assert.True(set.Add(20));

        Assert.Equal(2, set.Count);
        Assert.Equal(10, items[0]);
        Assert.Equal(20, items[1]);
    }

    /// <summary>
    /// Adding a duplicate returns false and leaves the count unchanged.
    /// </summary>
    [Fact]
    public void Add_DuplicateItem_ReturnsFalseAndKeepsCount()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        set.Add(1);

        Assert.False(set.Add(1));
        Assert.Equal(1, set.Count);
    }

    /// <summary>
    /// Adding a new item to a full set throws <see cref="InvalidOperationException"/> without modifying it.
    /// </summary>
    [Fact]
    public void Add_NewItemWhenFull_Throws()
    {
        Span<int> items = stackalloc int[2];
        Span<int> table = stackalloc int[4];
        var set = new SpanLargeSet<int>(items, table);
        set.Add(1);
        set.Add(2);

        try
        {
            set.Add(3);
            Assert.Fail("Expected InvalidOperationException.");
        }
        catch (InvalidOperationException)
        {
        }

        Assert.Equal(2, set.Count);
        Assert.False(set.Contains(3));
        Assert.True(set.Contains(1));
        Assert.True(set.Contains(2));
    }

    /// <summary>
    /// Adding an existing item to a full set returns false rather than throwing.
    /// </summary>
    [Fact]
    public void Add_DuplicateWhenFull_ReturnsFalse()
    {
        var set = new SpanLargeSet<int>(stackalloc int[1], stackalloc int[2]);
        set.Add(7);

        Assert.False(set.Add(7));
    }

    /// <summary>
    /// <see cref="SpanLargeSet{T}.TryAdd"/> reports new items, duplicates, and fullness without throwing.
    /// </summary>
    [Fact]
    public void TryAdd_ReturnsExpectedResults()
    {
        var set = new SpanLargeSet<int>(stackalloc int[2], stackalloc int[4]);

        Assert.True(set.TryAdd(1));
        Assert.False(set.TryAdd(1));
        Assert.True(set.TryAdd(2));
        Assert.False(set.TryAdd(3));
        Assert.False(set.TryAdd(1));
        Assert.Equal(2, set.Count);
    }

    /// <summary>
    /// Filling to capacity leaves every element reachable.
    /// </summary>
    [Fact]
    public void Add_LargeSequence_AllElementsReachable()
    {
        const int n = 4096;
        var items = new int[n];
        var table = new int[SpanLargeSet.RequiredTableLength(n)];
        var set = new SpanLargeSet<int>(items, table);

        for (var i = 0; i < n; i++)
        {
            Assert.True(set.Add(i * 7919));
        }

        Assert.True(set.IsFull());
        for (var i = 0; i < n; i++)
        {
            Assert.True(set.Contains(i * 7919));
            Assert.False(set.Add(i * 7919));
        }

        Assert.False(set.Contains(-1));
    }

    #endregion

    #region Hash Collisions

    /// <summary>
    /// Elements whose hash codes collide are still distinguished by equality.
    /// </summary>
    [Fact]
    public void Add_AllHashesCollide_StillDistinguishesElements()
    {
        var set = new SpanLargeSet<SameHash>(stackalloc SameHash[8], stackalloc int[16]);

        for (var i = 0; i < 8; i++)
        {
            Assert.True(set.Add(new SameHash(i)));
        }

        for (var i = 0; i < 8; i++)
        {
            Assert.True(set.Contains(new SameHash(i)));
            Assert.False(set.Add(new SameHash(i)));
        }

        Assert.False(set.Contains(new SameHash(8)));
    }

    /// <summary>
    /// Removing from the middle of a collision chain keeps later chain members reachable.
    /// </summary>
    [Fact]
    public void Remove_FromCollisionChain_KeepsRemainingReachable()
    {
        var set = new SpanLargeSet<SameHash>(stackalloc SameHash[8], stackalloc int[16]);
        for (var i = 0; i < 8; i++)
        {
            set.Add(new SameHash(i));
        }

        Assert.True(set.Remove(new SameHash(0)));
        Assert.True(set.Remove(new SameHash(3)));
        Assert.True(set.Remove(new SameHash(7)));

        Assert.Equal(5, set.Count);
        foreach (var key in new[] { 1, 2, 4, 5, 6 })
        {
            Assert.True(set.Contains(new SameHash(key)));
        }

        foreach (var key in new[] { 0, 3, 7 })
        {
            Assert.False(set.Contains(new SameHash(key)));
        }

        Assert.True(set.Add(new SameHash(0)));
        Assert.True(set.Add(new SameHash(3)));
        Assert.True(set.Add(new SameHash(7)));
        Assert.True(set.IsFull());
    }

    /// <summary>
    /// Chains that wrap around the end of the table survive backward-shift deletion.
    /// </summary>
    [Fact]
    public void Remove_WrappingChain_KeepsRemainingReachable()
    {
        // Home slot is the top bits of hash * Fibonacci; with a 16-slot table, keys are chosen
        // so that several share the final slots and the chain wraps to slot 0.
        var set = new SpanLargeSet<SameHash>(stackalloc SameHash[8], stackalloc int[16]);
        for (var i = 0; i < 8; i++)
        {
            set.Add(new SameHash(100 + i));
        }

        for (var i = 0; i < 8; i += 2)
        {
            Assert.True(set.Remove(new SameHash(100 + i)));
        }

        for (var i = 1; i < 8; i += 2)
        {
            Assert.True(set.Contains(new SameHash(100 + i)));
        }

        Assert.Equal(4, set.Count);
    }

    #endregion

    #region Clear

    /// <summary>
    /// <see cref="SpanLargeSet{T}.Clear"/> resets the count and forgets all members but keeps the item data.
    /// </summary>
    [Fact]
    public void Clear_ResetsCountAndMembership()
    {
        Span<int> items = stackalloc int[3];
        var set = new SpanLargeSet<int>(items, stackalloc int[8]);
        set.Add(1);
        set.Add(2);

        set.Clear();

        Assert.Equal(0, set.Count);
        Assert.False(set.Contains(1));
        Assert.Equal(1, items[0]);
        Assert.Equal(2, items[1]);
    }

    /// <summary>
    /// After clearing, previously present items can be added again.
    /// </summary>
    [Fact]
    public void Clear_ThenAdd_AllowsPreviousItemsAgain()
    {
        var set = new SpanLargeSet<int>(stackalloc int[3], stackalloc int[8]);
        set.Add(1);
        set.Add(2);
        set.Clear();

        Assert.True(set.Add(1));
        Assert.True(set.Add(2));
        Assert.True(set.Add(3));
        Assert.True(set.IsFull());
    }

    #endregion

    #region Contains

    /// <summary>
    /// Membership uses <see cref="IEquatable{T}"/> semantics of custom unmanaged structs.
    /// </summary>
    [Fact]
    public void Contains_WorksWithCustomEquatableStruct()
    {
        var set = new SpanLargeSet<Point>(stackalloc Point[3], stackalloc int[8]);
        set.Add(new Point(1, 2));
        set.Add(new Point(3, 4));

        Assert.True(set.Contains(new Point(3, 4)));
        Assert.False(set.Contains(new Point(5, 6)));
        Assert.False(set.Add(new Point(1, 2)));
    }

    /// <summary>
    /// Equality is driven by the custom <see cref="IEquatable{T}"/> implementation rather than by all fields.
    /// </summary>
    [Fact]
    public void Add_UsesCustomEqualityImplementation()
    {
        var set = new SpanLargeSet<KeyOnly>(stackalloc KeyOnly[2], stackalloc int[4]);

        Assert.True(set.Add(new KeyOnly(1, 100)));
        Assert.False(set.Add(new KeyOnly(1, 200)));
        Assert.True(set.Contains(new KeyOnly(1, 300)));
        Assert.Equal(1, set.Count);
    }

    /// <summary>
    /// Negative hash codes map into the table.
    /// </summary>
    [Fact]
    public void Contains_NegativeValues_Work()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        set.Add(int.MinValue);
        set.Add(-1);
        set.Add(0);

        Assert.True(set.Contains(int.MinValue));
        Assert.True(set.Contains(-1));
        Assert.True(set.Contains(0));
        Assert.False(set.Contains(int.MaxValue));
    }

    #endregion

    #region Remove

    /// <summary>
    /// <see cref="SpanLargeSet{T}.Remove"/> swaps the last element into the removed slot and re-indexes it.
    /// </summary>
    [Fact]
    public void Remove_ExistingItem_MovesLastIntoPlaceAndKeepsIndex()
    {
        Span<int> items = stackalloc int[4];
        var set = new SpanLargeSet<int>(items, stackalloc int[8]);
        set.Add(1);
        set.Add(2);
        set.Add(3);

        Assert.True(set.Remove(1));

        Assert.Equal(2, set.Count);
        Assert.Equal(3, items[0]);
        Assert.Equal(2, items[1]);
        Assert.False(set.Contains(1));
        Assert.True(set.Contains(2));
        Assert.True(set.Contains(3));
    }

    /// <summary>
    /// Removing a missing item returns false and changes nothing.
    /// </summary>
    [Fact]
    public void Remove_MissingItem_ReturnsFalseAndKeepsSet()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        set.Add(1);

        Assert.False(set.Remove(9));
        Assert.Equal(1, set.Count);
        Assert.True(set.Contains(1));
    }

    /// <summary>
    /// Removing from an empty set returns false.
    /// </summary>
    [Fact]
    public void Remove_OnEmptySet_ReturnsFalse()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);

        Assert.False(set.Remove(1));
    }

    /// <summary>
    /// Removing the last-added element does not disturb the others.
    /// </summary>
    [Fact]
    public void Remove_LastElement_KeepsOthers()
    {
        var set = new SpanLargeSet<int>(stackalloc int[3], stackalloc int[8]);
        set.Add(1);
        set.Add(2);
        set.Add(3);

        Assert.True(set.Remove(3));

        Assert.Equal(2, set.Count);
        Assert.True(set.Contains(1));
        Assert.True(set.Contains(2));
        Assert.False(set.Contains(3));
    }

    /// <summary>
    /// Removing the only element empties the set.
    /// </summary>
    [Fact]
    public void Remove_OnlyElement_EmptiesSet()
    {
        var set = new SpanLargeSet<int>(stackalloc int[2], stackalloc int[4]);
        set.Add(5);

        Assert.True(set.Remove(5));
        Assert.Equal(0, set.Count);
        Assert.False(set.Contains(5));
    }

    /// <summary>
    /// A full set accepts a new element after a removal.
    /// </summary>
    [Fact]
    public void Remove_FromFullSet_AllowsAddAgain()
    {
        var set = new SpanLargeSet<int>(stackalloc int[2], stackalloc int[4]);
        set.Add(1);
        set.Add(2);

        Assert.True(set.Remove(1));
        Assert.True(set.Add(3));
        Assert.True(set.IsFull());
        Assert.True(set.Contains(2));
        Assert.True(set.Contains(3));
    }

    /// <summary>
    /// Interleaved random adds and removes keep the index consistent with a reference <see cref="HashSet{T}"/>.
    /// </summary>
    [Fact]
    public void Remove_RandomizedWorkload_MatchesHashSet()
    {
        const int capacity = 512;
        var items = new int[capacity];
        var table = new int[SpanLargeSet.RequiredTableLength(capacity)];
        var set = new SpanLargeSet<int>(items, table);
        var reference = new HashSet<int>();
        var random = new Random(42);

        for (var step = 0; step < 20_000; step++)
        {
            var value = random.Next(0, capacity * 2);
            if (random.Next(3) == 0)
            {
                Assert.Equal(reference.Remove(value), set.Remove(value));
            }
            else if (reference.Count < capacity)
            {
                Assert.Equal(reference.Add(value), set.Add(value));
            }

            Assert.Equal(reference.Count, set.Count);
        }

        foreach (var value in reference)
        {
            Assert.True(set.Contains(value));
        }

        ReadOnlySpan<int> view = set;
        Assert.Equal(reference.Count, view.Length);
        foreach (var value in view)
        {
            Assert.Contains(value, reference);
        }
    }

    #endregion

    #region Enumeration / Conversion

    /// <summary>
    /// Enumeration visits each element exactly once.
    /// </summary>
    [Fact]
    public void Enumeration_VisitsEachElementOnce()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        set.Add(3);
        set.Add(1);
        set.Add(2);

        var seen = new List<int>();
        foreach (var item in set)
        {
            seen.Add(item);
        }

        Assert.Equal(new[] { 3, 1, 2 }, seen);
    }

    /// <summary>
    /// The implicit conversion exposes only the current elements.
    /// </summary>
    [Fact]
    public void ImplicitConversion_ToReadOnlySpan_CoversOnlyCurrentElements()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        set.Add(1);
        set.Add(2);

        ReadOnlySpan<int> view = set;

        Assert.Equal(2, view.Length);
        Assert.Equal(1, view[0]);
        Assert.Equal(2, view[1]);
    }

    #endregion

    #region Set Operations

    /// <summary>
    /// <see cref="SpanLargeSet{T}.UnionWith"/> adds only missing elements.
    /// </summary>
    [Fact]
    public void UnionWith_AddsOnlyMissingElements()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        set.Add(1);

        set.UnionWith(stackalloc int[] { 1, 2, 2, 3 });

        Assert.Equal(3, set.Count);
        Assert.True(set.SetEquals(stackalloc int[] { 1, 2, 3 }));
    }

    /// <summary>
    /// A union that exceeds capacity throws.
    /// </summary>
    [Fact]
    public void UnionWith_ExceedingCapacity_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            var set = new SpanLargeSet<int>(stackalloc int[2], stackalloc int[4]);
            set.UnionWith(stackalloc int[] { 1, 2, 3 });
        });
    }

    /// <summary>
    /// The span overload of <see cref="SpanLargeSet{T}.IntersectWith(ReadOnlySpan{T})"/> keeps only shared elements.
    /// </summary>
    [Fact]
    public void IntersectWith_Span_KeepsOnlySharedElements()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        set.UnionWith(stackalloc int[] { 1, 2, 3, 4 });

        set.IntersectWith(stackalloc int[] { 2, 4, 6 });

        Assert.Equal(2, set.Count);
        Assert.True(set.Contains(2));
        Assert.True(set.Contains(4));
        Assert.False(set.Contains(1));
        Assert.False(set.Contains(3));
    }

    /// <summary>
    /// Intersecting with an empty span empties the set.
    /// </summary>
    [Fact]
    public void IntersectWith_EmptySpan_EmptiesSet()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        set.Add(1);

        set.IntersectWith(ReadOnlySpan<int>.Empty);

        Assert.Equal(0, set.Count);
        Assert.False(set.Contains(1));
    }

    /// <summary>
    /// The set overload of <see cref="SpanLargeSet{T}.IntersectWith(SpanLargeSet{T})"/> keeps only shared elements.
    /// </summary>
    [Fact]
    public void IntersectWith_Set_KeepsOnlySharedElements()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        set.UnionWith(stackalloc int[] { 1, 2, 3, 4 });
        var other = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        other.UnionWith(stackalloc int[] { 3, 4, 5 });

        set.IntersectWith(other);

        Assert.True(set.SetEquals(stackalloc int[] { 3, 4 }));
    }

    /// <summary>
    /// <see cref="SpanLargeSet{T}.ExceptWith"/> removes the listed elements.
    /// </summary>
    [Fact]
    public void ExceptWith_RemovesListedElements()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        set.UnionWith(stackalloc int[] { 1, 2, 3, 4 });

        set.ExceptWith(stackalloc int[] { 2, 4, 9 });

        Assert.True(set.SetEquals(stackalloc int[] { 1, 3 }));
    }

    /// <summary>
    /// <see cref="SpanLargeSet{T}.Overlaps"/> reflects shared elements.
    /// </summary>
    [Fact]
    public void Overlaps_ReflectsSharedElements()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        set.UnionWith(stackalloc int[] { 1, 2 });

        Assert.True(set.Overlaps(stackalloc int[] { 9, 2 }));
        Assert.False(set.Overlaps(stackalloc int[] { 8, 9 }));
        Assert.False(set.Overlaps(ReadOnlySpan<int>.Empty));
    }

    /// <summary>
    /// An empty set overlaps nothing.
    /// </summary>
    [Fact]
    public void Overlaps_EmptySet_IsFalse()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);

        Assert.False(set.Overlaps(stackalloc int[] { 1 }));
    }

    /// <summary>
    /// <see cref="SpanLargeSet{T}.IsSubsetOf(ReadOnlySpan{T})"/> reflects containment.
    /// </summary>
    [Fact]
    public void IsSubsetOf_Span_ReflectsContainment()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        set.UnionWith(stackalloc int[] { 1, 2 });

        Assert.True(set.IsSubsetOf(stackalloc int[] { 2, 1, 3 }));
        Assert.False(set.IsSubsetOf(stackalloc int[] { 1 }));
        Assert.False(set.IsSubsetOf(stackalloc int[] { 1, 3 }));
    }

    /// <summary>
    /// <see cref="SpanLargeSet{T}.IsSubsetOf(SpanLargeSet{T})"/> reflects containment.
    /// </summary>
    [Fact]
    public void IsSubsetOf_Set_ReflectsContainment()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        set.UnionWith(stackalloc int[] { 1, 2 });
        var superset = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        superset.UnionWith(stackalloc int[] { 3, 2, 1 });
        var disjoint = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        disjoint.UnionWith(stackalloc int[] { 1, 3, 4 });

        Assert.True(set.IsSubsetOf(superset));
        Assert.False(set.IsSubsetOf(disjoint));
    }

    /// <summary>
    /// <see cref="SpanLargeSet{T}.IsSupersetOf"/> reflects containment and ignores duplicates.
    /// </summary>
    [Fact]
    public void IsSupersetOf_ReflectsContainment()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        set.UnionWith(stackalloc int[] { 1, 2, 3 });

        Assert.True(set.IsSupersetOf(stackalloc int[] { 1, 1, 2, 2, 3, 3 }));
        Assert.True(set.IsSupersetOf(ReadOnlySpan<int>.Empty));
        Assert.False(set.IsSupersetOf(stackalloc int[] { 1, 4 }));
    }

    /// <summary>
    /// <see cref="SpanLargeSet{T}.SetEquals(ReadOnlySpan{T})"/> ignores order and duplicates.
    /// </summary>
    [Fact]
    public void SetEquals_Span_IgnoresOrderAndDuplicates()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        set.UnionWith(stackalloc int[] { 1, 2, 3 });

        Assert.True(set.SetEquals(stackalloc int[] { 3, 1, 2, 2 }));
        Assert.False(set.SetEquals(stackalloc int[] { 1, 2 }));
        Assert.False(set.SetEquals(stackalloc int[] { 1, 2, 3, 4 }));
    }

    /// <summary>
    /// <see cref="SpanLargeSet{T}.SetEquals(SpanLargeSet{T})"/> compares membership regardless of insertion order.
    /// </summary>
    [Fact]
    public void SetEquals_Set_IgnoresOrder()
    {
        var set = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        set.UnionWith(stackalloc int[] { 1, 2, 3 });
        var same = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        same.UnionWith(stackalloc int[] { 3, 2, 1 });
        var different = new SpanLargeSet<int>(stackalloc int[4], stackalloc int[8]);
        different.UnionWith(stackalloc int[] { 1, 2, 4 });

        Assert.True(set.SetEquals(same));
        Assert.False(set.SetEquals(different));
    }

    #endregion

    private readonly record struct Point(int X, int Y);

    /// <summary>
    /// A struct whose equality considers only <see cref="Key"/>.
    /// </summary>
    private readonly struct KeyOnly : IEquatable<KeyOnly>
    {
        public KeyOnly(int key, int payload)
        {
            Key = key;
            Payload = payload;
        }

        public int Key { get; }

        public int Payload { get; }

        public bool Equals(KeyOnly other) => Key == other.Key;

        public override bool Equals(object? obj) => obj is KeyOnly other && Equals(other);

        public override int GetHashCode() => Key;
    }

    /// <summary>
    /// A struct whose every instance hashes to the same value, forcing probe chains.
    /// </summary>
    private readonly struct SameHash : IEquatable<SameHash>
    {
        public SameHash(int key)
        {
            Key = key;
        }

        public int Key { get; }

        public bool Equals(SameHash other) => Key == other.Key;

        public override bool Equals(object? obj) => obj is SameHash other && Equals(other);

        public override int GetHashCode() => unchecked((int)0xFFFFFFFF);
    }
}
