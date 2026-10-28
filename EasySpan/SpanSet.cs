using System;
using System.Runtime.InteropServices;

namespace EasySpan
{
    /// <summary>
    /// Represents a fixed-capacity set of unique elements backed by a caller-provided <see cref="Span{T}"/>.
    /// </summary>
    /// <remarks>
    /// Elements are stored linearly; membership checks are O(n). This type is intended for small sets
    /// in allocation-sensitive code paths, where it avoids the heap allocations of <c>HashSet&lt;T&gt;</c>.
    /// </remarks>
    /// <typeparam name="T">The unmanaged type of elements, implementing <see cref="IEquatable{T}"/>.</typeparam>
    public ref struct SpanSet<T> where T : unmanaged, IEquatable<T>
    {
        /// <summary>
        /// The backing <see cref="Span{T}"/> that stores the set elements.
        /// </summary>
        private readonly Span<T> _span;

        /// <summary>
        /// Gets the number of elements currently in the set.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Initializes an empty <see cref="SpanSet{T}"/> backed by the specified <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="span">The <see cref="Span{T}"/> used to store the set elements.</param>
        public SpanSet(Span<T> span)
        {
            _span = span;
            Count = 0;
        }

        /// <summary>
        /// Initializes a <see cref="SpanSet{T}"/> using the first specified number of elements in the backing <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="span">The <see cref="Span{T}"/> used to store the set elements.</param>
        /// <param name="count">The initial number of elements in the set.</param>
        /// <exception cref="ArgumentOutOfRangeException">The count exceeds the span length.</exception>
        /// <exception cref="ArgumentException">The first <paramref name="count"/> elements contain duplicates.</exception>
        public SpanSet(Span<T> span, int count)
        {
            _span = span;
            Count = count;
            if (Count > Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(count),
                    "Count cannot be greater than the capacity of the span.");
            }

            for (var i = 1; i < Count; i++)
            {
                if (IndexOf(_span[i], i) != -1)
                {
                    throw new ArgumentException("The initial elements must be unique.", nameof(span));
                }
            }
        }

        /// <summary>
        /// Gets the maximum number of elements the backing <see cref="Span{T}"/> can hold.
        /// </summary>
        public readonly int Capacity => _span.Length;

        /// <summary>
        /// Gets a read-only view over the first <see cref="Count"/> elements without a bounds check;
        /// the invariant <c>Count &lt;= Capacity</c> is maintained by every mutating member.
        /// </summary>
        private readonly ReadOnlySpan<T> Items => Prefix(Count);

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
            if (Contains(item))
            {
                return false;
            }

            if (IsFull())
            {
                throw new InvalidOperationException("SpanSet is full. Cannot add more items.");
            }

            _span[Count] = item;
            Count++;
            return true;
        }

        /// <summary>
        /// Attempts to add an element to the set without throwing when the set is full.
        /// </summary>
        /// <param name="item">The element to add.</param>
        /// <returns>True if the element was added; false if it was already present or the set is full.</returns>
        public bool TryAdd(T item)
        {
            if (IsFull() || Contains(item))
            {
                return false;
            }

            _span[Count] = item;
            Count++;
            return true;
        }

        /// <summary>
        /// Resets <see cref="Count"/> to zero without clearing the backing <see cref="Span{T}"/>.
        /// </summary>
        public void Clear()
        {
            Count = 0;
        }

        /// <summary>
        /// Determines whether the set contains the specified element.
        /// </summary>
        /// <param name="item">The element to locate.</param>
        /// <returns>True if the element is found; otherwise, false.</returns>
        public readonly bool Contains(T item)
        {
            return SpanSearch.Contains(Items, item);
        }

        /// <summary>
        /// Removes the specified element by replacing it with the last element, without preserving order.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns>True if the element was removed; otherwise, false.</returns>
        public bool Remove(T item)
        {
            var index = IndexOf(item, Count);
            if (index == -1)
            {
                return false;
            }

            Count--;
            _span[index] = _span[Count];
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
                if (!SpanSearch.Contains(other, _span[i]))
                {
                    Count--;
                    _span[i] = _span[Count];
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
        /// Determines whether every element of the specified span is present in the set.
        /// </summary>
        /// <remarks>Duplicate elements in the specified span are ignored.</remarks>
        /// <param name="other">The elements to compare against.</param>
        /// <returns>True if the set is a superset of <paramref name="other"/>; otherwise, false.</returns>
        public readonly bool IsSupersetOf(ReadOnlySpan<T> other)
        {
            var self = Items;
            foreach (var item in other)
            {
                if (!SpanSearch.Contains(self, item))
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
        /// Converts the <see cref="SpanSet{T}"/> to a <see cref="ReadOnlySpan{T}"/> containing its current elements.
        /// </summary>
        /// <param name="spanSet">The <see cref="SpanSet{T}"/> to convert.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> over the first <see cref="Count"/> elements of the backing span.</returns>
        public static implicit operator ReadOnlySpan<T>(SpanSet<T> spanSet)
        {
            return spanSet.Items;
        }

        /// <summary>
        /// Finds the index of the specified element among the first <paramref name="length"/> stored elements.
        /// </summary>
        /// <param name="item">The element to locate.</param>
        /// <param name="length">The number of leading elements to search.</param>
        /// <returns>The index of the element, or -1 if it is not found.</returns>
        private readonly int IndexOf(T item, int length)
        {
            return Prefix(length).IndexOf(item);
        }

        /// <summary>
        /// Creates a read-only view over the first <paramref name="length"/> elements of the backing span
        /// without a bounds check. Callers must guarantee <c>0 &lt;= length &lt;= Capacity</c>.
        /// </summary>
        /// <param name="length">The number of leading elements to expose.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> over the leading elements.</returns>
        private readonly ReadOnlySpan<T> Prefix(int length)
        {
            return MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetReference(_span), length);
        }
    }
}
