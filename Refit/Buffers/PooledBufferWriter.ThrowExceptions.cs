using System;
using System.Runtime.CompilerServices;

namespace Refit.Buffers
{
    internal sealed partial class PooledBufferWriter
    {
        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> when a method receives a negative "count" parameter.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentOutOfRangeExceptionForNegativeCount()
        {
            throw new ArgumentOutOfRangeException("count", "The count can't be < 0");
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> when a method receives a negative "offset" parameter.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentOutOfRangeExceptionForNegativeOffset()
        {
            throw new ArgumentOutOfRangeException("offset", "The offset can't be < 0");
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> when <see cref="Advance"/> advances too far.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentOutOfRangeExceptionForAdvancedTooFar()
        {
            throw new ArgumentOutOfRangeException("count", "Advanced too far");
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> when the end of a <see cref="PooledMemoryStream"/> has been exceeded.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentOutOfRangeExceptionForEndOfStreamReached()
        {
            throw new ArgumentException("The end of the stream has been exceeded");
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> when a <see cref="PooledMemoryStream"/> method is called on a disposed instance.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException("The stream in use has alreadybeen disposed");
        }

        /// <summary>
        /// Throws an <see cref="NotSupportedException"/> when an operation in <see cref="PooledMemoryStream"/> is not supported.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotSupportedException()
        {
            throw new NotSupportedException("The stream doesn't support the requested operation");
        }
    }
}
