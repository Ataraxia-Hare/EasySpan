namespace EasySpan.Tests;

/// <summary>
/// Unit tests for <see cref="SpanSet{T}"/>.
/// </summary>
public class SpanSetTests
{
    #region Constructors

    /// <summary>
    /// The span-only constructor creates an empty set whose capacity equals the span length.
    /// </summary>
    [Fact]
    public void Constructor_WithSpan_CreatesEmptySet()
    {
        var set = new SpanSet<int>(stackalloc int[4]);

        Assert.Equal(0, set.Count);
        Assert.Equal(4, set.Capacity);
        Assert.False(set.IsFull());
    }

    /// <summary>
    /// A set over an empty span has zero capacity and is both empty and full.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptySpan_IsFullAndEmpty()
    {
        var set = new SpanSet<int>(Span<int>.Empty);

        Assert.Equal(0, set.Count);
        Assert.Equal(0, set.Capacity);
        Assert.True(set.IsFull());
    }

    /// <summary>
    /// The count constructor exposes the first <paramref name="count"/> existing unique elements of the span.
    /// </summary>
    /// <param name="count">The initial element count.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public void Constructor_WithCount_SetsCountAndKeepsExistingData(int count)
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2, 3, 4 }, count);

        Assert.Equal(count, set.Count);
        Assert.Equal(4, set.Capacity);
        for (var i = 1; i <= count; i++)
        {
            Assert.True(set.Contains(i));
        }
    }

    /// <summary>
    /// A count greater than the span length throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [Fact]
    public void Constructor_WithCountGreaterThanCapacity_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            Span<int> buffer = stackalloc int[3];
            _ = new SpanSet<int>(buffer, 4);
        });
        Assert.Equal("count", ex.ParamName);
    }

    /// <summary>
    /// Duplicate values within the initial elements throw <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void Constructor_WithDuplicateInitialElements_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            Span<int> buffer = stackalloc int[] { 1, 2, 1 };
            _ = new SpanSet<int>(buffer, 3);
        });
        Assert.Equal("span", ex.ParamName);
    }

    /// <summary>
    /// Duplicates located beyond the initial count are ignored by the constructor.
    /// </summary>
    [Fact]
    public void Constructor_WithDuplicatesBeyondCount_Succeeds()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2, 1 }, 2);

        Assert.Equal(2, set.Count);
    }

    #endregion

    #region Add / TryAdd

    /// <summary>
    /// <see cref="SpanSet{T}.Add"/> stores new items in the backing span and returns true.
    /// </summary>
    [Fact]
    public void Add_NewItems_ReturnsTrueAndWritesToBackingSpan()
    {
        Span<int> buffer = stackalloc int[] { 0, 0, 0 };
        var set = new SpanSet<int>(buffer);

        Assert.True(set.Add(10));
        Assert.True(set.Add(20));

        Assert.Equal(2, set.Count);
        Assert.True(buffer.SequenceEqual(stackalloc int[] { 10, 20, 0 }));
    }

    /// <summary>
    /// Adding a duplicate returns false and does not change the count.
    /// </summary>
    [Fact]
    public void Add_DuplicateItem_ReturnsFalseAndKeepsCount()
    {
        var set = new SpanSet<int>(stackalloc int[3]);
        set.Add(1);

        Assert.False(set.Add(1));
        Assert.Equal(1, set.Count);
    }

    /// <summary>
    /// Repeatedly adding the same items never grows the set beyond the distinct count.
    /// </summary>
    [Fact]
    public void Add_ManyDuplicates_KeepsOnlyDistinctItems()
    {
        var set = new SpanSet<int>(stackalloc int[4]);

        for (var i = 0; i < 20; i++)
        {
            set.Add(i % 3);
        }

        Assert.Equal(3, set.Count);
        Assert.True(set.Contains(0));
        Assert.True(set.Contains(1));
        Assert.True(set.Contains(2));
    }

    /// <summary>
    /// The set reports full once the number of distinct items reaches the capacity.
    /// </summary>
    [Fact]
    public void Add_UntilCapacity_BecomesFull()
    {
        var set = new SpanSet<int>(stackalloc int[2]);

        set.Add(1);
        Assert.False(set.IsFull());
        set.Add(2);

        Assert.True(set.IsFull());
    }

    /// <summary>
    /// Adding a new item to a full set throws <see cref="InvalidOperationException"/> and leaves the data untouched.
    /// </summary>
    [Fact]
    public void Add_NewItemWhenFull_Throws()
    {
        Span<int> buffer = stackalloc int[1];
        var set = new SpanSet<int>(buffer);
        set.Add(1);

        InvalidOperationException? exception = null;
        try
        {
            set.Add(2);
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(1, set.Count);
        Assert.Equal(1, buffer[0]);
    }

    /// <summary>
    /// Adding an existing item to a full set returns false instead of throwing.
    /// </summary>
    [Fact]
    public void Add_DuplicateWhenFull_ReturnsFalse()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2 }, 2);

        Assert.False(set.Add(1));
        Assert.Equal(2, set.Count);
    }

    /// <summary>
    /// Adding to a zero-capacity set throws <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void Add_ToZeroCapacitySet_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            var set = new SpanSet<int>(Span<int>.Empty);
            set.Add(1);
        });
    }

    /// <summary>
    /// <see cref="SpanSet{T}.TryAdd"/> adds new items and rejects duplicates without throwing.
    /// </summary>
    [Fact]
    public void TryAdd_NewAndDuplicateItems_ReturnsExpectedResults()
    {
        var set = new SpanSet<int>(stackalloc int[2]);

        Assert.True(set.TryAdd(1));
        Assert.False(set.TryAdd(1));
        Assert.Equal(1, set.Count);
    }

    /// <summary>
    /// <see cref="SpanSet{T}.TryAdd"/> returns false when the set is full instead of throwing.
    /// </summary>
    [Fact]
    public void TryAdd_WhenFull_ReturnsFalse()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1 }, 1);

        Assert.False(set.TryAdd(2));
        Assert.False(set.TryAdd(1));
        Assert.Equal(1, set.Count);
        Assert.False(set.Contains(2));
    }

    #endregion

    #region Clear

    /// <summary>
    /// <see cref="SpanSet{T}.Clear"/> resets the count without modifying the backing span.
    /// </summary>
    [Fact]
    public void Clear_ResetsCountButKeepsBackingData()
    {
        Span<int> buffer = stackalloc int[] { 0, 0, 0 };
        var set = new SpanSet<int>(buffer);
        set.Add(1);
        set.Add(2);

        set.Clear();

        Assert.Equal(0, set.Count);
        Assert.Equal(3, set.Capacity);
        Assert.True(buffer.SequenceEqual(stackalloc int[] { 1, 2, 0 }));
    }

    /// <summary>
    /// After clearing, previously present items can be added again.
    /// </summary>
    [Fact]
    public void Clear_ThenAdd_AllowsPreviousItemsAgain()
    {
        var set = new SpanSet<int>(stackalloc int[2]);
        set.Add(1);
        set.Add(2);

        set.Clear();

        Assert.False(set.Contains(1));
        Assert.True(set.Add(1));
        Assert.Equal(1, set.Count);
    }

    /// <summary>
    /// Clearing an empty set is a no-op.
    /// </summary>
    [Fact]
    public void Clear_OnEmptySet_DoesNothing()
    {
        var set = new SpanSet<int>(stackalloc int[2]);

        set.Clear();

        Assert.Equal(0, set.Count);
    }

    #endregion

    #region Contains

    /// <summary>
    /// <see cref="SpanSet{T}.Contains"/> only considers elements within the current count.
    /// </summary>
    [Fact]
    public void Contains_ReflectsPresenceWithinCount()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2, 3 }, 2);

        Assert.True(set.Contains(1));
        Assert.True(set.Contains(2));
        Assert.False(set.Contains(3));
    }

    /// <summary>
    /// Searching an empty set returns false.
    /// </summary>
    [Fact]
    public void Contains_OnEmptySet_ReturnsFalse()
    {
        var set = new SpanSet<int>(stackalloc int[] { 0, 0 });

        Assert.False(set.Contains(0));
    }

    /// <summary>
    /// Membership uses <see cref="IEquatable{T}"/> semantics of custom unmanaged structs.
    /// </summary>
    [Fact]
    public void Contains_WorksWithCustomEquatableStruct()
    {
        var set = new SpanSet<Point>(stackalloc Point[3]);
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
        var set = new SpanSet<KeyOnly>(stackalloc KeyOnly[2]);

        Assert.True(set.Add(new KeyOnly(1, 100)));
        Assert.False(set.Add(new KeyOnly(1, 200)));
        Assert.True(set.Contains(new KeyOnly(1, 300)));
        Assert.Equal(1, set.Count);
    }

    #endregion

    #region Remove

    /// <summary>
    /// <see cref="SpanSet{T}.Remove"/> removes the item and returns true.
    /// </summary>
    [Fact]
    public void Remove_ExistingItem_ReturnsTrueAndRemovesItem()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2, 3, 4 }, 4);

        Assert.True(set.Remove(2));

        Assert.Equal(3, set.Count);
        Assert.False(set.Contains(2));
        Assert.True(set.Contains(1));
        Assert.True(set.Contains(3));
        Assert.True(set.Contains(4));
    }

    /// <summary>
    /// Removing an absent item returns false and leaves the set unchanged.
    /// </summary>
    [Fact]
    public void Remove_MissingItem_ReturnsFalseAndKeepsSet()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2, 3 }, 3);

        Assert.False(set.Remove(9));
        Assert.Equal(3, set.Count);
    }

    /// <summary>
    /// Removing from an empty set returns false.
    /// </summary>
    [Fact]
    public void Remove_OnEmptySet_ReturnsFalse()
    {
        var set = new SpanSet<int>(stackalloc int[] { 0, 0 });

        Assert.False(set.Remove(0));
    }

    /// <summary>
    /// Removing the only element leaves the set empty.
    /// </summary>
    [Fact]
    public void Remove_OnlyElement_EmptiesSet()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1 }, 1);

        set.Remove(1);

        Assert.Equal(0, set.Count);
    }

    /// <summary>
    /// Removing from a full set frees a slot so that <see cref="SpanSet{T}.Add"/> succeeds again.
    /// </summary>
    [Fact]
    public void Remove_FromFullSet_AllowsAddAgain()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2 }, 2);

        set.Remove(1);
        set.Add(5);

        Assert.True(set.IsFull());
        Assert.True(set.Contains(5));
        Assert.True(set.Contains(2));
    }

    /// <summary>
    /// A removed item can be added again and is counted once.
    /// </summary>
    [Fact]
    public void Remove_ThenAddSameItem_Succeeds()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2, 3 }, 3);

        set.Remove(1);

        Assert.True(set.Add(1));
        Assert.Equal(3, set.Count);
    }

    #endregion

    #region Enumeration / Conversion

    /// <summary>
    /// <c>foreach</c> visits each current element exactly once.
    /// </summary>
    [Fact]
    public void Enumeration_VisitsEachElementOnce()
    {
        var set = new SpanSet<int>(stackalloc int[4]);
        set.Add(3);
        set.Add(1);
        set.Add(2);

        Span<int> visited = stackalloc int[3];
        var count = 0;
        foreach (var item in set)
        {
            visited[count++] = item;
        }

        Assert.Equal(3, count);
        visited.Sort();
        Assert.True(visited.SequenceEqual(stackalloc int[] { 1, 2, 3 }));
    }

    /// <summary>
    /// Enumerating an empty set yields nothing.
    /// </summary>
    [Fact]
    public void Enumeration_OnEmptySet_YieldsNothing()
    {
        var set = new SpanSet<int>(stackalloc int[3]);

        var count = 0;
        foreach (var _ in set)
        {
            count++;
        }

        Assert.Equal(0, count);
    }

    /// <summary>
    /// Enumeration excludes elements in the backing span beyond <see cref="SpanSet{T}.Count"/>.
    /// </summary>
    [Fact]
    public void Enumeration_IgnoresItemsBeyondCount()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2, 3 }, 2);

        var sum = 0;
        foreach (var item in set)
        {
            sum += item;
        }

        Assert.Equal(3, sum);
    }

    /// <summary>
    /// Conversion to <see cref="ReadOnlySpan{T}"/> covers only the current elements.
    /// </summary>
    [Fact]
    public void ImplicitConversion_ToReadOnlySpan_CoversOnlyCurrentElements()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2, 3, 4 }, 2);

        ReadOnlySpan<int> span = set;

        Assert.Equal(2, span.Length);
        Assert.True(span.SequenceEqual(stackalloc int[] { 1, 2 }));
    }

    /// <summary>
    /// Converting an empty set yields an empty span.
    /// </summary>
    [Fact]
    public void ImplicitConversion_OnEmptySet_ReturnsEmptySpan()
    {
        var set = new SpanSet<int>(stackalloc int[3]);

        ReadOnlySpan<int> span = set;

        Assert.True(span.IsEmpty);
    }

    /// <summary>
    /// A converted span reflects the set state after additions and removals.
    /// </summary>
    [Fact]
    public void ImplicitConversion_ReflectsLaterChanges()
    {
        var set = new SpanSet<int>(stackalloc int[3]);
        set.Add(1);
        set.Add(2);
        set.Remove(1);

        ReadOnlySpan<int> span = set;

        Assert.True(span.SequenceEqual(stackalloc int[] { 2 }));
    }

    #endregion

    #region Set Operations

    /// <summary>
    /// <see cref="SpanSet{T}.UnionWith"/> adds only the missing elements.
    /// </summary>
    [Fact]
    public void UnionWith_AddsOnlyMissingElements()
    {
        var set = new SpanSet<int>(stackalloc int[5]);
        set.Add(1);
        set.Add(2);

        ReadOnlySpan<int> other = stackalloc int[] { 2, 3, 3, 4 };
        set.UnionWith(other);

        Assert.Equal(4, set.Count);
        Assert.True(set.SetEquals(stackalloc int[] { 1, 2, 3, 4 }));
    }

    /// <summary>
    /// <see cref="SpanSet{T}.UnionWith"/> with an empty span is a no-op.
    /// </summary>
    [Fact]
    public void UnionWith_EmptySpan_DoesNothing()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2 }, 2);

        set.UnionWith(ReadOnlySpan<int>.Empty);

        Assert.Equal(2, set.Count);
    }

    /// <summary>
    /// <see cref="SpanSet{T}.UnionWith"/> throws when the new elements exceed the remaining capacity.
    /// </summary>
    [Fact]
    public void UnionWith_ExceedingCapacity_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            Span<int> buffer = stackalloc int[2];
            var set = new SpanSet<int>(buffer);
            ReadOnlySpan<int> other = stackalloc int[] { 1, 2, 3 };
            set.UnionWith(other);
        });
    }

    /// <summary>
    /// <see cref="SpanSet{T}.IntersectWith"/> keeps only the shared elements.
    /// </summary>
    [Fact]
    public void IntersectWith_KeepsOnlySharedElements()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2, 3, 4 }, 4);

        ReadOnlySpan<int> other = stackalloc int[] { 2, 4, 6 };
        set.IntersectWith(other);

        Assert.Equal(2, set.Count);
        Assert.True(set.SetEquals(stackalloc int[] { 2, 4 }));
    }

    /// <summary>
    /// <see cref="SpanSet{T}.IntersectWith"/> with an empty span empties the set.
    /// </summary>
    [Fact]
    public void IntersectWith_EmptySpan_EmptiesSet()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2 }, 2);

        set.IntersectWith(ReadOnlySpan<int>.Empty);

        Assert.Equal(0, set.Count);
    }

    /// <summary>
    /// <see cref="SpanSet{T}.IntersectWith"/> with a superset leaves the set unchanged.
    /// </summary>
    [Fact]
    public void IntersectWith_Superset_KeepsAllElements()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2 }, 2);

        ReadOnlySpan<int> other = stackalloc int[] { 0, 1, 2, 3 };
        set.IntersectWith(other);

        Assert.Equal(2, set.Count);
        Assert.True(set.SetEquals(stackalloc int[] { 1, 2 }));
    }

    /// <summary>
    /// <see cref="SpanSet{T}.ExceptWith"/> removes the listed elements and ignores absent ones.
    /// </summary>
    [Fact]
    public void ExceptWith_RemovesListedElements()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2, 3, 4 }, 4);

        ReadOnlySpan<int> other = stackalloc int[] { 2, 4, 9 };
        set.ExceptWith(other);

        Assert.Equal(2, set.Count);
        Assert.True(set.SetEquals(stackalloc int[] { 1, 3 }));
    }

    /// <summary>
    /// <see cref="SpanSet{T}.ExceptWith"/> with an empty span is a no-op.
    /// </summary>
    [Fact]
    public void ExceptWith_EmptySpan_DoesNothing()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2 }, 2);

        set.ExceptWith(ReadOnlySpan<int>.Empty);

        Assert.Equal(2, set.Count);
    }

    /// <summary>
    /// <see cref="SpanSet{T}.Overlaps"/> reports whether any element is shared.
    /// </summary>
    [Fact]
    public void Overlaps_ReflectsSharedElements()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2, 3 }, 3);

        Assert.True(set.Overlaps(stackalloc int[] { 9, 3 }));
        Assert.False(set.Overlaps(stackalloc int[] { 7, 8 }));
        Assert.False(set.Overlaps(ReadOnlySpan<int>.Empty));
    }

    /// <summary>
    /// <see cref="SpanSet{T}.Overlaps"/> finds shared elements when the other span is shorter than the set.
    /// </summary>
    [Fact]
    public void Overlaps_OtherShorterThanSet_ReflectsSharedElements()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2, 3, 4 }, 4);

        Assert.True(set.Overlaps(stackalloc int[] { 9, 4 }));
        Assert.True(set.Overlaps(stackalloc int[] { 1 }));
        Assert.False(set.Overlaps(stackalloc int[] { 9, 8 }));
    }

    /// <summary>
    /// <see cref="SpanSet{T}.Overlaps"/> finds shared elements when the other span is longer than the set.
    /// </summary>
    [Fact]
    public void Overlaps_OtherLongerThanSet_ReflectsSharedElements()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2 }, 2);

        Assert.True(set.Overlaps(stackalloc int[] { 9, 8, 7, 2 }));
        Assert.True(set.Overlaps(stackalloc int[] { 1, 8, 7, 6 }));
        Assert.False(set.Overlaps(stackalloc int[] { 9, 8, 7, 6 }));
    }

    /// <summary>
    /// An empty set never overlaps, even with a non-empty span.
    /// </summary>
    [Fact]
    public void Overlaps_EmptySet_IsFalse()
    {
        var set = new SpanSet<int>(stackalloc int[2]);

        Assert.False(set.Overlaps(stackalloc int[] { 1, 2 }));
        Assert.False(set.Overlaps(ReadOnlySpan<int>.Empty));
    }

    /// <summary>
    /// <see cref="SpanSet{T}.IsSubsetOf"/> is true when every element is present in the other span.
    /// </summary>
    [Fact]
    public void IsSubsetOf_ReflectsContainment()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2 }, 2);

        Assert.True(set.IsSubsetOf(stackalloc int[] { 2, 1, 3 }));
        Assert.True(set.IsSubsetOf(stackalloc int[] { 1, 2 }));
        Assert.False(set.IsSubsetOf(stackalloc int[] { 1 }));
    }

    /// <summary>
    /// An empty set is a subset of any span.
    /// </summary>
    [Fact]
    public void IsSubsetOf_EmptySet_IsAlwaysTrue()
    {
        var set = new SpanSet<int>(stackalloc int[2]);

        Assert.True(set.IsSubsetOf(ReadOnlySpan<int>.Empty));
        Assert.True(set.IsSubsetOf(stackalloc int[] { 1 }));
    }

    /// <summary>
    /// <see cref="SpanSet{T}.IsSupersetOf"/> is true when every element of the other span is present.
    /// </summary>
    [Fact]
    public void IsSupersetOf_ReflectsContainment()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2, 3 }, 3);

        Assert.True(set.IsSupersetOf(stackalloc int[] { 3, 1 }));
        Assert.True(set.IsSupersetOf(stackalloc int[] { 1, 1, 2 }));
        Assert.True(set.IsSupersetOf(ReadOnlySpan<int>.Empty));
        Assert.False(set.IsSupersetOf(stackalloc int[] { 1, 4 }));
    }

    /// <summary>
    /// Repeated elements do not affect containment, even when the span is longer than the set.
    /// </summary>
    [Fact]
    public void IsSupersetOf_WithMoreDuplicatesThanCount_IgnoresDuplicates()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2 }, 2);

        Assert.True(set.IsSupersetOf(stackalloc int[] { 2, 1, 2, 1 }));
        Assert.False(set.IsSupersetOf(stackalloc int[] { 1, 1, 3, 3 }));
    }

    /// <summary>
    /// <see cref="SpanSet{T}.SetEquals"/> ignores order and duplicates in the other span.
    /// </summary>
    [Fact]
    public void SetEquals_IgnoresOrderAndDuplicates()
    {
        var set = new SpanSet<int>(stackalloc int[] { 1, 2, 3 }, 3);

        Assert.True(set.SetEquals(stackalloc int[] { 3, 1, 2 }));
        Assert.True(set.SetEquals(stackalloc int[] { 1, 1, 2, 3, 3 }));
        Assert.False(set.SetEquals(stackalloc int[] { 1, 2 }));
        Assert.False(set.SetEquals(stackalloc int[] { 1, 2, 3, 4 }));
        Assert.False(set.SetEquals(stackalloc int[] { 1, 1, 2, 2 }));
        Assert.False(set.SetEquals(stackalloc int[] { 1, 2, 4 }));
    }

    /// <summary>
    /// An empty set equals only an empty span.
    /// </summary>
    [Fact]
    public void SetEquals_EmptySet_EqualsOnlyEmptySpan()
    {
        var set = new SpanSet<int>(stackalloc int[2]);

        Assert.True(set.SetEquals(ReadOnlySpan<int>.Empty));
        Assert.False(set.SetEquals(stackalloc int[] { 1 }));
    }

    #endregion

    /// <summary>
    /// The set works over a <c>stackalloc</c> buffer.
    /// </summary>
    [Fact]
    public void WorksWithStackallocBuffer()
    {
        Span<int> buffer = stackalloc int[3];
        var set = new SpanSet<int>(buffer);

        set.Add(1);
        set.Add(2);
        set.Add(2);
        set.Add(3);

        Assert.True(set.IsFull());
        Assert.True(set.Contains(2));
    }

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
}
