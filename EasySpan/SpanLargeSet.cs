using System;
using System.Runtime.CompilerServices;

namespace EasySpan
{
    /// <summary>
    /// Provides factory and sizing helpers for <see cref="SpanLargeSet{T}"/>.
    /// </summary>
    public static class SpanLargeSet
    {
        /// <summary>
        /// The smallest index table length accepted by <see cref="SpanLargeSet{T}"/>.
        /// </summary>
        public const int MinTableLength = 2;

        /// <summary>
        /// Returns the smallest valid index table length for a set of the specified capacity.
        /// </summary>
        /// <remarks>
        /// The result is a power of two that is at least twice <paramref name="capacity"/>, keeping the
        /// load factor at or below one half so that probe sequences stay short and always terminate.
        /// </remarks>
        /// <param name="capacity">The maximum number of elements the set will hold.</param>
        /// <returns>The required length of the <c>Span&lt;int&gt;</c> index table.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The capacity is negative or too large to index.</exception>
        public static int RequiredTableLength(int capacity)
        {
            const int maxCapacity = 1 << 29;
            if ((uint)capacity > maxCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity),
                    "Capacity must be between 0 and 2^29.");
            }

            var length = MinTableLength;
            while (length < capacity * 2)
            {
                length <<= 1;
            }

            return length;
        }

        /// <summary>
        /// Creates a <see cref="SpanLargeSet{T}"/> over the first <paramref name="count"/> elements of
        /// <paramref name="items"/> without validating that they are unique.
        /// </summary>
        /// <remarks>
        /// The index table is rebuilt in O(n). The caller must guarantee that the first <paramref name="count"/>
        /// elements are distinct; duplicates leave the set in an inconsistent state.
        /// </remarks>
        /// <typeparam name="T">The unmanaged type of elements, implementing <see cref="IEquatable{T}"/>.</typeparam>
        /// <param name="items">The <see cref="Span{T}"/> used to store the set elements.</param>
        /// <param name="table">The index table; see <see cref="RequiredTableLength"/>.</param>
        /// <param name="count">The initial number of elements in the set.</param>
        /// <returns>A set exposing the leading elements of <paramref name="items"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The count exceeds the span length.</exception>
        /// <exception cref="ArgumentException">The table length is not a power of two of sufficient size.</exception>
        public static SpanLargeSet<T> CreateUnchecked<T>(Span<T> items, Span<int> table, int count)
            where T : unmanaged, IEquatable<T>
        {
            return new SpanLargeSet<T>(items, table, count, validate: false);
        }
    }

    /// <summary>
    /// Represents a fixed-capacity set of unique elements backed by caller-provided spans, using an
    /// open-addressing hash index for constant-time membership.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Elements are stored contiguously in <c>items</c>, exactly like <see cref="SpanSet{T}"/>, so enumeration
    /// and conversion to <see cref="ReadOnlySpan{T}"/> are allocation-free views. A second caller-provided
    /// <c>Span&lt;int&gt;</c> holds the hash index: each slot stores <c>elementIndex + 1</c>, with zero meaning empty.
    /// The table length must be a power of two that is at least twice the item capacity; use
    /// <see cref="SpanLargeSet.RequiredTableLength"/> to size it.
    /// </para>
    /// <para>
    /// <see cref="Add"/>, <see cref="Contains"/>, and <see cref="Remove"/> are O(1) on average. This type is intended
    /// for large sets in allocation-sensitive code; for a few dozen elements <see cref="SpanSet{T}"/> is usually
    /// faster because it avoids hashing entirely.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The unmanaged type of elements, implementing <see cref="IEquatable{T}"/>.</typeparam>
    public ref struct SpanLargeSet<T> where T : unmanaged, IEquatable<T>
    {
        private const uint FibonacciMultiplier = 0x9E3779B1u;

        private readonly Span<T> _items;
        private readonly Span<int> _table;
        private readonly int _shift;

        /// <summary>
        /// Initializes an empty <see cref="SpanLargeSet{T}"/> backed by the specified spans.
        /// </summary>
        /// <param name="items">The <see cref="Span{T}"/> used to store the set elements.</param>
        /// <param name="table">The index table; see <see cref="SpanLargeSet.RequiredTableLength"/>.</param>
        /// <exception cref="ArgumentException">The table length is not a power of two of sufficient size.</exception>
        public SpanLargeSet(Span<T> items, Span<int> table)
        {
            ValidateTable(items, table);
            _items = items;
            _table = table;
            _shift = ComputeShift(table.Length);
            Count = 0;
            table.Clear();
        }

        /// <summary>
        /// Initializes a <see cref="SpanLargeSet{T}"/> using the first specified number of elements in <paramref name="items"/>.
        /// </summary>
        /// <remarks>The index table is rebuilt and the initial elements are validated for uniqueness in O(n).</remarks>
        /// <param name="items">The <see cref="Span{T}"/> used to store the set elements.</param>
        /// <param name="table">The index table; see <see cref="SpanLargeSet.RequiredTableLength"/>.</param>
        /// <param name="count">The initial number of elements in the set.</param>
        /// <exception cref="ArgumentOutOfRangeException">The count exceeds the span length.</exception>
        /// <exception cref="ArgumentException">
        /// The table length is invalid, or the first <paramref name="count"/> elements contain duplicates.
        /// </exception>
        public SpanLargeSet(Span<T> items, Span<int> table, int count)
            : this(items, table, count, validate: true)
        {
        }

        internal SpanLargeSet(Span<T> items, Span<int> table, int count, bool validate)
        {
            ValidateTable(items, table);
            if (count > items.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count),
                    "Count cannot be greater than the capacity of the span.");
            }

            _items = items;
            _table = table;
            _shift = ComputeShift(table.Length);
            Count = count;
            table.Clear();

            var mask = table.Length - 1;
            for (var i = 0; i < count; i++)
            {
                var item = SpanRef.At(items, i);
                var slot = Home(item);
                while (true)
                {
                    ref var entry = ref SpanRef.At(table, slot);
                    if (entry == 0)
                    {
                        entry = i + 1;
                        break;
                    }

                    if (validate && SpanRef.At(items, entry - 1).Equals(item))
                    {
                        throw new ArgumentException("The initial elements must be unique.", nameof(items));
                    }

                    slot = (slot + 1) & mask;
                }
            }
        }

        /// <summary>
        /// Gets the number of elements currently in the set.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Gets the maximum number of elements the backing <see cref="Span{T}"/> can hold.
        /// </summary>
        public readonly int Capacity => _items.Length;

        /// <summary>
        /// Gets a read-only view over the first <see cref="Count"/> elements.
        /// </summary>
        private readonly ReadOnlySpan<T> Items => _items[..Count];

        /// <summary>
        /// Determines whether <see cref="Count"/> equals <see cref="Capacity"/>.
        /// </summary>
        /// <returns>True if the count equals the capacity; otherwise, false.</returns>
        public readonly bool IsFull()
        {
            return Capacity == Count;
        }

        #region Collection Methods

        /// <summary>
        /// Adds an element to the set if it is not already present.
        /// </summary>
        /// <param name="item">The element to add.</param>
        /// <returns>True if the element was added; false if it was already present.</returns>
        /// <exception cref="InvalidOperationException">The element is not present and the set is full.</exception>
        public bool Add(T item)
        {
            var slot = FindSlotOrEmpty(item, out var found);
            if (found)
            {
                return false;
            }

            if (Count == _items.Length)
            {
                throw new InvalidOperationException("SpanLargeSet is full. Cannot add more items.");
            }

            Insert(slot, item);
            return true;
        }

        /// <summary>
        /// Attempts to add an element to the set without throwing when the set is full.
        /// </summary>
        /// <param name="item">The element to add.</param>
        /// <returns>True if the element was added; false if it was already present or the set is full.</returns>
        public bool TryAdd(T item)
        {
            var slot = FindSlotOrEmpty(item, out var found);
            if (found || Count == _items.Length)
            {
                return false;
            }

            Insert(slot, item);
            return true;
        }

        /// <summary>
        /// Resets <see cref="Count"/> to zero and empties the index table without clearing the element span.
        /// </summary>
        public void Clear()
        {
            _table.Clear();
            Count = 0;
        }

        /// <summary>
        /// Determines whether the set contains the specified element.
        /// </summary>
        /// <param name="item">The element to locate.</param>
        /// <returns>True if the element is found; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(T item)
        {
            var mask = _table.Length - 1;
            var slot = Home(item);
            while (true)
            {
                var entry = SpanRef.At(_table, slot);
                if (entry == 0)
                {
                    return false;
                }

                if (SpanRef.At(_items, entry - 1).Equals(item))
                {
                    return true;
                }

                slot = (slot + 1) & mask;
            }
        }

        /// <summary>
        /// Removes the specified element by replacing it with the last element, without preserving order.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns>True if the element was removed; otherwise, false.</returns>
        public bool Remove(T item)
        {
            var slot = FindSlotOrEmpty(item, out var found);
            if (!found)
            {
                return false;
            }

            var mask = _table.Length - 1;
            var index = SpanRef.At(_table, slot) - 1;

            // Backward-shift deletion keeps probe chains contiguous without tombstones.
            var hole = slot;
            var next = (hole + 1) & mask;
            while (true)
            {
                var entry = SpanRef.At(_table, next);
                if (entry == 0)
                {
                    break;
                }

                var home = Home(SpanRef.At(_items, entry - 1));
                if (((next - home) & mask) >= ((next - hole) & mask))
                {
                    SpanRef.At(_table, hole) = entry;
                    hole = next;
                }

                next = (next + 1) & mask;
            }

            SpanRef.At(_table, hole) = 0;

            Count--;
            if (index != Count)
            {
                var last = SpanRef.At(_items, Count);
                SpanRef.At(_items, index) = last;

                var moved = Home(last);
                while (SpanRef.At(_table, moved) != Count + 1)
                {
                    moved = (moved + 1) & mask;
                }

                SpanRef.At(_table, moved) = index + 1;
            }

            return true;
        }

        #endregion

        #region Set Methods

        /// <summary>
        /// Adds all elements of the specified span that are not already present.
        /// </summary>
        /// <param name="other">The elements to add.</param>
        /// <exception cref="InvalidOperationException">The set becomes full before all new elements are added.</exception>
        public void UnionWith(ReadOnlySpan<T> other)
        {
            foreach (var item in other)
            {
                Add(item);
            }
        }

        /// <summary>
        /// Removes all elements that are not present in the specified span.
        /// </summary>
        /// <remarks>This overload scans <paramref name="other"/> for each element and is O(n·m).</remarks>
        /// <param name="other">The elements to keep.</param>
        public void IntersectWith(ReadOnlySpan<T> other)
        {
            if (other.Length == 0)
            {
                Clear();
                return;
            }

            for (var i = Count - 1; i >= 0; i--)
            {
                var item = SpanRef.At(_items, i);
                if (!SpanSearch.Contains(other, item))
                {
                    Remove(item);
                }
            }
        }

        /// <summary>
        /// Removes all elements that are not present in the specified set.
        /// </summary>
        /// <param name="other">The set whose elements to keep.</param>
        public void IntersectWith(SpanLargeSet<T> other)
        {
            if (other.Count == 0)
            {
                Clear();
                return;
            }

            for (var i = Count - 1; i >= 0; i--)
            {
                var item = SpanRef.At(_items, i);
                if (!other.Contains(item))
                {
                    Remove(item);
                }
            }
        }

        /// <summary>
        /// Removes all elements that are present in the specified span.
        /// </summary>
        /// <param name="other">The elements to remove.</param>
        public void ExceptWith(ReadOnlySpan<T> other)
        {
            foreach (var item in other)
            {
                Remove(item);
            }
        }

        /// <summary>
        /// Determines whether the set shares at least one element with the specified span.
        /// </summary>
        /// <param name="other">The elements to compare against.</param>
        /// <returns>True if at least one element is common; otherwise, false.</returns>
        public readonly bool Overlaps(ReadOnlySpan<T> other)
        {
            if (Count == 0)
            {
                return false;
            }

            foreach (var item in other)
            {
                if (Contains(item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether every element of the set is present in the specified span.
        /// </summary>
        /// <remarks>This overload scans <paramref name="other"/> for each element and is O(n·m).</remarks>
        /// <param name="other">The elements to compare against.</param>
        /// <returns>True if the set is a subset of <paramref name="other"/>; otherwise, false.</returns>
        public readonly bool IsSubsetOf(ReadOnlySpan<T> other)
        {
            if (other.Length < Count)
            {
                return false;
            }

            foreach (var item in Items)
            {
                if (!SpanSearch.Contains(other, item))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether every element of the set is present in the specified set.
        /// </summary>
        /// <param name="other">The set to compare against.</param>
        /// <returns>True if the set is a subset of <paramref name="other"/>; otherwise, false.</returns>
        public readonly bool IsSubsetOf(SpanLargeSet<T> other)
        {
            if (other.Count < Count)
            {
                return false;
            }

            foreach (var item in Items)
            {
                if (!other.Contains(item))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether every element of the specified span is present in the set.
        /// </summary>
        /// <remarks>Duplicate elements in the specified span are ignored.</remarks>
        /// <param name="other">The elements to compare against.</param>
        /// <returns>True if the set is a superset of <paramref name="other"/>; otherwise, false.</returns>
        public readonly bool IsSupersetOf(ReadOnlySpan<T> other)
        {
            foreach (var item in other)
            {
                if (!Contains(item))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the set and the specified span contain the same distinct elements, ignoring order and duplicates.
        /// </summary>
        /// <param name="other">The elements to compare against.</param>
        /// <returns>True if both contain the same distinct elements; otherwise, false.</returns>
        public readonly bool SetEquals(ReadOnlySpan<T> other)
        {
            return IsSupersetOf(other) && IsSubsetOf(other);
        }

        /// <summary>
        /// Determines whether the set and the specified set contain the same elements, ignoring order.
        /// </summary>
        /// <param name="other">The set to compare against.</param>
        /// <returns>True if both contain the same elements; otherwise, false.</returns>
        public readonly bool SetEquals(SpanLargeSet<T> other)
        {
            return Count == other.Count && IsSubsetOf(other);
        }

        #endregion

        /// <summary>
        /// Returns an enumerator over the current elements of the set.
        /// </summary>
        /// <returns>An allocation-free enumerator for use with <c>foreach</c>.</returns>
        public readonly ReadOnlySpan<T>.Enumerator GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        /// <summary>
        /// Converts the <see cref="SpanLargeSet{T}"/> to a <see cref="ReadOnlySpan{T}"/> containing its current elements.
        /// </summary>
        /// <param name="set">The <see cref="SpanLargeSet{T}"/> to convert.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> over the first <see cref="Count"/> elements of the backing span.</returns>
        public static implicit operator ReadOnlySpan<T>(SpanLargeSet<T> set)
        {
            return set.Items;
        }

        /// <summary>
        /// Computes the ideal table slot of an element using Fibonacci hashing, which scatters
        /// sequential hash codes across the table.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly int Home(T item)
        {
            return (int)(unchecked((uint)item.GetHashCode() * FibonacciMultiplier) >> _shift);
        }

        /// <summary>
        /// Probes for the element and returns either its slot or the first empty slot of its chain.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly int FindSlotOrEmpty(T item, out bool found)
        {
            var mask = _table.Length - 1;
            var slot = Home(item);
            while (true)
            {
                var entry = SpanRef.At(_table, slot);
                if (entry == 0)
                {
                    found = false;
                    return slot;
                }

                if (SpanRef.At(_items, entry - 1).Equals(item))
                {
                    found = true;
                    return slot;
                }

                slot = (slot + 1) & mask;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Insert(int slot, T item)
        {
            SpanRef.At(_items, Count) = item;
            Count++;
            SpanRef.At(_table, slot) = Count;
        }

        private static void ValidateTable(ReadOnlySpan<T> items, Span<int> table)
        {
            var length = table.Length;
            if (length < SpanLargeSet.MinTableLength || (length & (length - 1)) != 0)
            {
                throw new ArgumentException("Table length must be a power of two of at least 2.", nameof(table));
            }

            if (length < items.Length * 2L)
            {
                throw new ArgumentException("Table length must be at least twice the item capacity.", nameof(table));
            }
        }

        private static int ComputeShift(int tableLength)
        {
            var bits = 0;
            while (1 << bits < tableLength)
            {
                bits++;
            }

            return 32 - bits;
        }
    }
}
