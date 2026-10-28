namespace EasySpan.Tests;

/// <summary>
/// Unit tests for <see cref="SpanList{T}"/>.
/// </summary>
public class SpanListTests
{
    #region Constructors

    /// <summary>
    /// The span-only constructor creates an empty list whose capacity equals the span length.
    /// </summary>
    [Fact]
    public void Constructor_WithSpan_CreatesEmptyList()
    {
        var list = new SpanList<int>(stackalloc int[4]);

        Assert.Equal(0, list.Count);
        Assert.Equal(4, list.Capacity);
        Assert.False(list.IsFull());
    }

    /// <summary>
    /// A list over an empty span has zero capacity and is both empty and full.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptySpan_IsFullAndEmpty()
    {
        var list = new SpanList<int>(Span<int>.Empty);

        Assert.Equal(0, list.Count);
        Assert.Equal(0, list.Capacity);
        Assert.True(list.IsFull());
    }

    /// <summary>
    /// The count constructor exposes the first <paramref name="count"/> existing elements of the span.
    /// </summary>
    /// <param name="count">The initial element count.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public void Constructor_WithCount_SetsCountAndKeepsExistingData(int count)
    {
        var list = new SpanList<int>(stackalloc int[] { 1, 2, 3, 4 }, count);

        Assert.Equal(count, list.Count);
        Assert.Equal(4, list.Capacity);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i + 1, list[i]);
        }
    }

    /// <summary>
    /// A count greater than the span length throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [Fact]
    public void Constructor_WithCountGreaterThanCapacity_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new SpanList<int>(stackalloc int[3], 4));
        Assert.Equal("count", ex.ParamName);
    }

    #endregion

    #region IsFull / Add

    /// <summary>
    /// <see cref="SpanList{T}.Add"/> appends items in order and writes them to the backing span.
    /// </summary>
    [Fact]
    public void Add_AppendsItemsInOrderAndWritesToBackingSpan()
    {
        Span<int> buffer = stackalloc int[] { 0, 0, 0 };
        var list = new SpanList<int>(buffer);

        list.Add(10);
        list.Add(20);

        Assert.Equal(2, list.Count);
        Assert.Equal(10, list[0]);
        Assert.Equal(20, list[1]);
        Assert.True(buffer.SequenceEqual(stackalloc int[] { 10, 20, 0 }));
    }

    /// <summary>
    /// The list reports full once the number of added items reaches the capacity.
    /// </summary>
    [Fact]
    public void Add_UntilCapacity_BecomesFull()
    {
        var list = new SpanList<int>(stackalloc int[2]);

        list.Add(1);
        Assert.False(list.IsFull());
        list.Add(2);

        Assert.True(list.IsFull());
    }

    /// <summary>
    /// Adding to a full list throws <see cref="InvalidOperationException"/> and leaves the data untouched.
    /// </summary>
    [Fact]
    public void Add_WhenFull_Throws()
    {
        Span<int> buffer = stackalloc int[1];
        var list = new SpanList<int>(buffer);
        list.Add(1);

        InvalidOperationException? exception = null;
        try
        {
            list.Add(2);
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(1, list.Count);
        Assert.Equal(1, buffer[0]);
    }

    /// <summary>
    /// Adding to a zero-capacity list throws <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void Add_ToZeroCapacityList_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            var list = new SpanList<int>(Span<int>.Empty);
            list.Add(1);
        });
    }

    #endregion

    #region Clear

    /// <summary>
    /// <see cref="SpanList{T}.Clear"/> resets the count without modifying the backing span.
    /// </summary>
    [Fact]
    public void Clear_ResetsCountButKeepsBackingData()
    {
        Span<int> buffer = stackalloc int[] { 0, 0, 0 };
        var list = new SpanList<int>(buffer);
        list.Add(1);
        list.Add(2);

        list.Clear();

        Assert.Equal(0, list.Count);
        Assert.Equal(3, list.Capacity);
        Assert.True(buffer.SequenceEqual(stackalloc int[] { 1, 2, 0 }));
    }

    /// <summary>
    /// After clearing, new items reuse the existing storage from the start.
    /// </summary>
    [Fact]
    public void Clear_ThenAdd_ReusesStorage()
    {
        var list = new SpanList<int>(stackalloc int[2]);
        list.Add(1);
        list.Add(2);

        list.Clear();
        list.Add(9);

        Assert.Equal(1, list.Count);
        Assert.Equal(9, list[0]);
        Assert.False(list.IsFull());
    }

    /// <summary>
    /// Clearing an empty list is a no-op.
    /// </summary>
    [Fact]
    public void Clear_OnEmptyList_DoesNothing()
    {
        var list = new SpanList<int>(stackalloc int[2]);

        list.Clear();

        Assert.Equal(0, list.Count);
    }

    #endregion

    #region IndexOf / Contains

    /// <summary>
    /// <see cref="SpanList{T}.IndexOf"/> returns the index of the first matching element.
    /// </summary>
    [Fact]
    public void IndexOf_ReturnsFirstMatch()
    {
        var list = new SpanList<int>(stackalloc int[] { 5, 7, 5, 9 }, 4);

        Assert.Equal(0, list.IndexOf(5));
        Assert.Equal(1, list.IndexOf(7));
        Assert.Equal(3, list.IndexOf(9));
    }

    /// <summary>
    /// <see cref="SpanList{T}.IndexOf"/> returns -1 when the item is absent.
    /// </summary>
    [Fact]
    public void IndexOf_MissingItem_ReturnsMinusOne()
    {
        var list = new SpanList<int>(stackalloc int[] { 1, 2, 3 }, 3);

        Assert.Equal(-1, list.IndexOf(4));
    }

    /// <summary>
    /// Elements in the backing span beyond <see cref="SpanList{T}.Count"/> are not searched.
    /// </summary>
    [Fact]
    public void IndexOf_IgnoresItemsBeyondCount()
    {
        var list = new SpanList<int>(stackalloc int[] { 1, 2, 3, 4 }, 2);

        Assert.Equal(-1, list.IndexOf(3));
        Assert.Equal(-1, list.IndexOf(4));
    }

    /// <summary>
    /// Searching an empty list returns -1.
    /// </summary>
    [Fact]
    public void IndexOf_OnEmptyList_ReturnsMinusOne()
    {
        var list = new SpanList<int>(stackalloc int[] { 0, 0 });

        Assert.Equal(-1, list.IndexOf(0));
    }

    /// <summary>
    /// <see cref="SpanList{T}.Contains"/> only considers elements within the current count.
    /// </summary>
    [Fact]
    public void Contains_ReflectsPresenceWithinCount()
    {
        var list = new SpanList<int>(stackalloc int[] { 1, 2, 3 }, 2);

        Assert.True(list.Contains(1));
        Assert.True(list.Contains(2));
        Assert.False(list.Contains(3));
    }

    /// <summary>
    /// Searching works with custom unmanaged <see cref="IEquatable{T}"/> structs.
    /// </summary>
    [Fact]
    public void IndexOf_WorksWithCustomEquatableStruct()
    {
        var list = new SpanList<Point>(stackalloc Point[3]);
        list.Add(new Point(1, 2));
        list.Add(new Point(3, 4));

        Assert.Equal(1, list.IndexOf(new Point(3, 4)));
        Assert.False(list.Contains(new Point(5, 6)));
    }

    #endregion

    #region Remove / RemoveAt

    /// <summary>
    /// <see cref="SpanList{T}.RemoveAt"/> fills the removed slot with the last element.
    /// </summary>
    [Fact]
    public void RemoveAt_ReplacesWithLastElement()
    {
        var list = new SpanList<int>(stackalloc int[] { 1, 2, 3, 4 }, 4);

        list.RemoveAt(1);

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(4, list[1]);
        Assert.Equal(3, list[2]);
    }

    /// <summary>
    /// Removing the last element simply shrinks the count.
    /// </summary>
    [Fact]
    public void RemoveAt_LastElement_ShrinksCount()
    {
        var list = new SpanList<int>(stackalloc int[] { 1, 2, 3 }, 3);

        list.RemoveAt(2);

        Assert.Equal(2, list.Count);
        Assert.Equal(-1, list.IndexOf(3));
    }

    /// <summary>
    /// Removing the only element leaves the list empty.
    /// </summary>
    [Fact]
    public void RemoveAt_OnlyElement_EmptiesList()
    {
        var list = new SpanList<int>(stackalloc int[] { 1 }, 1);

        list.RemoveAt(0);

        Assert.Equal(0, list.Count);
    }

    /// <summary>
    /// An index outside the current elements throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    /// <param name="index">The invalid index.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RemoveAt_InvalidIndex_Throws(int index)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            Span<int> buffer = stackalloc int[] { 1, 2, 3 };
            var list = new SpanList<int>(buffer, 2);
            list.RemoveAt(index);
        });
        Assert.Equal("index", ex.ParamName);
    }

    /// <summary>
    /// Removing by index from an empty list throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [Fact]
    public void RemoveAt_OnEmptyList_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            Span<int> buffer = stackalloc int[2];
            var list = new SpanList<int>(buffer);
            list.RemoveAt(0);
        });
    }

    /// <summary>
    /// <see cref="SpanList{T}.Remove"/> removes the first match and returns true.
    /// </summary>
    [Fact]
    public void Remove_ExistingItem_ReturnsTrueAndRemovesFirstMatch()
    {
        var list = new SpanList<int>(stackalloc int[] { 1, 2, 1, 3 }, 4);

        var removed = list.Remove(1);

        Assert.True(removed);
        Assert.Equal(3, list.Count);
        Assert.Equal(3, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(1, list[2]);
    }

    /// <summary>
    /// Removing an absent item returns false and leaves the list unchanged.
    /// </summary>
    [Fact]
    public void Remove_MissingItem_ReturnsFalseAndKeepsList()
    {
        var list = new SpanList<int>(stackalloc int[] { 1, 2, 3 }, 3);

        var removed = list.Remove(9);

        Assert.False(removed);
        Assert.Equal(3, list.Count);
    }

    /// <summary>
    /// Removing from an empty list returns false.
    /// </summary>
    [Fact]
    public void Remove_OnEmptyList_ReturnsFalse()
    {
        var list = new SpanList<int>(stackalloc int[] { 0, 0 });

        Assert.False(list.Remove(0));
    }

    /// <summary>
    /// Removing from a full list frees a slot so that <see cref="SpanList{T}.Add"/> succeeds again.
    /// </summary>
    [Fact]
    public void Remove_FromFullList_AllowsAddAgain()
    {
        var list = new SpanList<int>(stackalloc int[] { 1, 2 }, 2);

        list.Remove(1);
        list.Add(5);

        Assert.True(list.IsFull());
        Assert.True(list.Contains(5));
        Assert.True(list.Contains(2));
    }

    #endregion

    #region Indexer

    /// <summary>
    /// The indexer returns a reference that writes through to the backing span.
    /// </summary>
    [Fact]
    public void Indexer_ReturnsWritableReference()
    {
        Span<int> buffer = stackalloc int[] { 1, 2, 3 };
        var list = new SpanList<int>(buffer, 3);

        list[1] = 42;

        Assert.Equal(42, buffer[1]);
        Assert.Equal(42, list[1]);
    }

    /// <summary>
    /// The indexer is bounded by the span length, not by <see cref="SpanList{T}.Count"/>.
    /// </summary>
    [Fact]
    public void Indexer_CanAccessBackingSpanBeyondCount()
    {
        var list = new SpanList<int>(stackalloc int[] { 1, 2, 3 }, 1);

        Assert.Equal(3, list[2]);
    }

    /// <summary>
    /// An index outside the backing span throws <see cref="IndexOutOfRangeException"/>.
    /// </summary>
    /// <param name="index">The invalid index.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void Indexer_OutsideCapacity_Throws(int index)
    {
        Assert.Throws<IndexOutOfRangeException>(() =>
        {
            Span<int> buffer = stackalloc int[3];
            var list = new SpanList<int>(buffer);
            return list[index];
        });
    }

    #endregion

    #region Implicit conversions

    /// <summary>
    /// Conversion to <see cref="Span{T}"/> covers only the current elements and shares storage.
    /// </summary>
    [Fact]
    public void ImplicitConversion_ToSpan_CoversOnlyCurrentElements()
    {
        Span<int> buffer = stackalloc int[] { 1, 2, 3, 4 };
        var list = new SpanList<int>(buffer, 3);

        Span<int> span = list;

        Assert.Equal(3, span.Length);
        Assert.True(span.SequenceEqual(stackalloc int[] { 1, 2, 3 }));
        span[0] = 99;
        Assert.Equal(99, buffer[0]);
    }

    /// <summary>
    /// Conversion to <see cref="ReadOnlySpan{T}"/> covers only the current elements.
    /// </summary>
    [Fact]
    public void ImplicitConversion_ToReadOnlySpan_CoversOnlyCurrentElements()
    {
        var list = new SpanList<int>(stackalloc int[] { 1, 2, 3, 4 }, 2);

        ReadOnlySpan<int> span = list;

        Assert.Equal(2, span.Length);
        Assert.True(span.SequenceEqual(stackalloc int[] { 1, 2 }));
    }

    /// <summary>
    /// Converting an empty list yields empty spans.
    /// </summary>
    [Fact]
    public void ImplicitConversion_OnEmptyList_ReturnsEmptySpan()
    {
        var list = new SpanList<int>(stackalloc int[3]);

        Span<int> span = list;
        ReadOnlySpan<int> readOnly = list;

        Assert.True(span.IsEmpty);
        Assert.True(readOnly.IsEmpty);
    }

    /// <summary>
    /// A converted span reflects the list state after additions and removals.
    /// </summary>
    [Fact]
    public void ImplicitConversion_ReflectsLaterChanges()
    {
        var list = new SpanList<int>(stackalloc int[3]);
        list.Add(1);
        list.Add(2);
        list.Remove(1);

        ReadOnlySpan<int> span = list;

        Assert.True(span.SequenceEqual(stackalloc int[] { 2 }));
    }

    #endregion

    /// <summary>
    /// The list works over a <c>stackalloc</c> buffer.
    /// </summary>
    [Fact]
    public void WorksWithStackallocBuffer()
    {
        Span<int> buffer = stackalloc int[3];
        var list = new SpanList<int>(buffer);

        list.Add(1);
        list.Add(2);
        list.Add(3);

        Assert.True(list.IsFull());
        Assert.Equal(1, list.IndexOf(2));
    }

    private readonly record struct Point(int X, int Y);
}
