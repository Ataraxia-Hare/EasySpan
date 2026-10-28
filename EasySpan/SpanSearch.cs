using System;
using System.Runtime.CompilerServices;

namespace EasySpan
{
    /// <summary>
    /// Membership helpers shared by the span collections that pick the fastest search available on the target framework.
    /// </summary>
    internal static class SpanSearch
    {
        /// <summary>
        /// Determines whether the span contains the specified value.
        /// </summary>
        /// <remarks>
        /// On .NET this forwards to <c>MemoryExtensions.Contains</c>, which skips the index computation of
        /// <c>IndexOf</c>; .NET Standard 2.1 lacks that overload and falls back to <c>IndexOf</c>.
        /// </remarks>
        /// <typeparam name="T">The unmanaged element type.</typeparam>
        /// <param name="span">The elements to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True if the value is present; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>(ReadOnlySpan<T> span, T value) where T : unmanaged, IEquatable<T>
        {
#if NETSTANDARD2_1
            return span.IndexOf(value) != -1;
#else
            return MemoryExtensions.Contains(span, value);
#endif
        }
    }
}
