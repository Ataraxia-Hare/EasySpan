using System;
using System.Runtime.CompilerServices;
#if !NETSTANDARD2_1
using System.Runtime.InteropServices;
#endif

namespace EasySpan
{
    /// <summary>
    /// Element access helpers shared by the span collections for hot paths whose indices are proven in range.
    /// </summary>
    internal static class SpanRef
    {
        /// <summary>
        /// Returns a reference to the element at the specified index.
        /// </summary>
        /// <remarks>
        /// On .NET this skips the bounds check; the caller must guarantee <paramref name="index"/> is valid.
        /// .NET Standard 2.1 lacks <c>Unsafe</c> in the box and falls back to checked indexing.
        /// </remarks>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="span">The span to access.</param>
        /// <param name="index">The zero-based index, which must be less than the span length.</param>
        /// <returns>A reference to the element.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T At<T>(Span<T> span, int index)
        {
#if NETSTANDARD2_1
            return ref span[index];
#else
            return ref Unsafe.Add(ref MemoryMarshal.GetReference(span), index);
#endif
        }
    }
}
