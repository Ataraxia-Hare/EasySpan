using System;

namespace EasySpan
{
    /// <summary>
    /// Represents a fixed-capacity list backed by a caller-provided <see cref="Span{T}"/>.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of elements, implementing <see cref="IEquatable{T}"/>.</typeparam>
    public ref struct SpanList<T> where T : unmanaged, IEquatable<T>
    {
        /// <summary>
        /// The backing <see cref="Span{T}"/> that stores the list elements.
        /// </summary>
        private readonly Span<T> _span;

        /// <summary>
        /// Gets the number of elements currently in the list.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Initializes an empty <see cref="SpanList{T}"/> backed by the specified <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="span">The <see cref="Span{T}"/> used to store the list elements.</param>
        public SpanList(Span<T> span)
        {
            _span = span;
            Count = 0;
        }

        /// <summary>
        /// Initializes a <see cref="SpanList{T}"/> using the first specified number of elements in the backing <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="span">The <see cref="Span{T}"/> used to store the list elements.</param>
        /// <param name="count">The initial number of elements in the list.</param>
        /// <exception cref="ArgumentOutOfRangeException">The count exceeds the span length.</exception>
        public SpanList(Span<T> span, int count)
        {
            _span = span;
            Count = count;
            if (Count > Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(count),
                    "Count cannot be greater than the capacity of the span.");
            }
        }

        /// <summary>
        /// Gets the maximum number of elements the backing <see cref="Span{T}"/> can hold.
        /// </summary>
        public readonly int Capacity => _span.Length;

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
        /// Adds an element to the end of the list.
        /// </summary>
        /// <param name="item">The element to add.</param>
        /// <exception cref="InvalidOperationException">The list is full.</exception>
        public void Add(T item)
        {
            if (IsFull())
            {
                throw new InvalidOperationException("SpanList is full. Cannot add more items.");
            }

            _span[Count] = item;
            Count++;
        }

        /// <summary>
        /// Resets <see cref="Count"/> to zero without clearing the backing <see cref="Span{T}"/>.
        /// </summary>
        public void Clear()
        {
            Count = 0;
        }

        /// <summary>
        /// Determines whether the list contains the specified element.
        /// </summary>
        /// <param name="item">The element to locate.</param>
        /// <returns>True if the element is found; otherwise, false.</returns>
        public readonly bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        /// <summary>
        /// Removes the first matching element by replacing it with the last element, without preserving order.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns>True if an element was removed; otherwise, false.</returns>
        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index == -1)
            {
                return false;
            }

            RemoveAt(index);
            return true;
        }

        #endregion

        #region List Methods

        /// <summary>
        /// Gets a reference to the element at the specified index in the backing <see cref="Span{T}"/>, regardless of <see cref="Count"/>.
        /// </summary>
        /// <param name="index">The zero-based index in the backing span.</param>
        /// <returns>A writable reference to the element at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">The index is outside the backing span.</exception>
        public readonly ref T this[int index] => ref _span[index];

        /// <summary>
        /// Finds the zero-based index of the first matching element in the list.
        /// </summary>
        /// <param name="item">The element to locate.</param>
        /// <returns>The index of the first matching element, or -1 if no match is found.</returns>
        public readonly int IndexOf(T item)
        {
            for (var index = 0; index < Count; index++)
            {
                if (_span[index].Equals(item))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Removes the element at the specified index by replacing it with the last element, without preserving order.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">The index is outside the current list elements.</exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            }

            Count--;
            _span[index] = _span[Count];
        }

        #endregion

        /// <summary>
        /// Converts the <see cref="SpanList{T}"/> to a <see cref="Span{T}"/> containing its current elements.
        /// </summary>
        /// <param name="spanList">The <see cref="SpanList{T}"/> to convert.</param>
        /// <returns>A <see cref="Span{T}"/> over the first <see cref="Count"/> elements of the backing span.</returns>
        public static implicit operator Span<T>(SpanList<T> spanList)
        {
            return spanList._span[..spanList.Count];
        }

        /// <summary>
        /// Converts the <see cref="SpanList{T}"/> to a <see cref="ReadOnlySpan{T}"/> containing its current elements.
        /// </summary>
        /// <param name="spanList">The <see cref="SpanList{T}"/> to convert.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> over the first <see cref="Count"/> elements of the backing span.</returns>
        public static implicit operator ReadOnlySpan<T>(SpanList<T> spanList)
        {
            return spanList._span[..spanList.Count];
        }
    }
}
