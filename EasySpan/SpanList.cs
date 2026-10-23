using System;

namespace EasySpan
{
    public ref struct SpanList<T> where T : unmanaged, IEquatable<T>
    {
        private readonly Span<T> _span;

        public int Count { get; private set; }

        public SpanList(Span<T> span)
        {
            _span = span;
            Count = 0;
        }

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

        public readonly int Capacity => _span.Length;

        public readonly bool IsFull()
        {
            return Capacity == Count;
        }

        #region Collection Methods

        public void Add(T item)
        {
            if (IsFull())
            {
                throw new InvalidOperationException("SpanList is full. Cannot add more items.");
            }

            _span[Count] = item;
            Count++;
        }

        public void Clear()
        {
            Count = 0;
        }

        public readonly bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

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

        public readonly ref T this[int index] => ref _span[index];

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

        public static implicit operator Span<T>(SpanList<T> spanList)
        {
            return spanList._span[..spanList.Count];
        }

        public static implicit operator ReadOnlySpan<T>(SpanList<T> spanList)
        {
            return spanList._span[..spanList.Count];
        }
    }
}