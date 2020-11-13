using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Unsafe utility extensions.
    /// </summary>
    [BurstCompatible]
    public unsafe static class UnsafeUtilityExtensions
    {
        /// <summary>
        /// Swaps content of two buffers.
        /// </summary>
        /// <param name="destination">Destination memory pointer.</param>
        /// <param name="source">Source memory pointer.</param>
        /// <param name="size">Size.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if source and destination memory regions overlap.</exception>
        internal static void MemSwap(void* destination, void* source, long size)
        {
            byte* dst = (byte*) destination;
            byte* src = (byte*) source;

            CheckMemSwapOverlap(dst, src, size);

            var tmp = stackalloc byte[1024];

            while (size > 0)
            {
                var numBytes = math.min(size, 1024);
                UnsafeUtility.MemCpy(tmp, dst, numBytes);
                UnsafeUtility.MemCpy(dst, src, numBytes);
                UnsafeUtility.MemCpy(src, tmp, numBytes);

                size -= numBytes;
                src += numBytes;
                dst += numBytes;
            }
        }

        /// <summary>
        /// Reads an element to an unsafe buffer after bounds checking.
        /// </summary>
        /// <typeparam name="T">Type of data in the array.</typeparam>
        /// <param name="source">Source memory pointer.</param>
        /// <param name="index">Index into array.</param>
        /// <param name="capacity">Array capacity, used for bounds checking.</param>
        /// <returns>Element read from the array.</returns>
        /// <exception cref="System.IndexOutOfRangeException">Thrown if reading outside of the array's range.</exception>
        /// <remarks>Reading data out of bounds from an unsafe buffer can lead to crashes and data corruption.
        /// <seealso cref="UnsafeUtility.ReadArrayElement{T}(void*, int)"/> does not do any bounds checking, so it's fast, but provides no debugging or safety capabilities.
        /// This function provides basic bounds checking for <seealso cref="UnsafeUtility.ReadArrayElement{T}(void*, int)"/> and should be used when debuggability is required over performance.</remarks>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public unsafe static T ReadArrayElementBoundsChecked<T>(void* source, int index, int capacity)
        {
            CheckIndexRange(index, capacity);

            return UnsafeUtility.ReadArrayElement<T>(source, index);
        }

        /// <summary>
        /// Writes an element to an unsafe buffer after bounds checking.
        /// </summary>
        /// <typeparam name="T">Type of data in the array.</typeparam>
        /// <param name="destination">Destination memory pointer.</param>
        /// <param name="index">Index into array.</param>
        /// <param name="value">Value to write into array.</param>
        /// <param name="capacity">Array capacity, used for bounds checking.</param>
        /// <exception cref="System.IndexOutOfRangeException">Thrown if element would be written outside of the array's range.</exception>
        /// <remarks>Writing data out of bounds to an unsafe buffer can lead to crashes and data corruption.
        /// <seealso cref="UnsafeUtility.WriteArrayElement{T}(void*, int, T)"/> does not do any bounds checking, so it's fast, but provides no debugging or safety capabilities.
        /// This function provides basic bounds checking for <seealso cref="UnsafeUtility.WriteArrayElement{T}(void*, int, T)"/> and should be used when debuggability is required over performance.</remarks>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public unsafe static void WriteArrayElementBoundsChecked<T>(void* destination, int index, T value, int capacity)
        {
            CheckIndexRange(index, capacity);

            UnsafeUtility.WriteArrayElement<T>(destination, index, value);
        }

        /// <summary>
        /// Return the address of the read-only "in" reference parameter.
        /// </summary>
        /// <typeparam name="T">Type of the parameter.</typeparam>
        /// <param name="item">The read-only reference to a valuetype of type T.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public static void* AddressOf<T>(in T item)
            where T : struct
        {
            return ILSupport.AddressOf(in item);
        }

        /// <summary>
        /// Erases the "read-only" "in" part of the given reference argument, and returns a regular ref to it.
        /// Useful to avoid a defensive copy when calling methods on "in" args.  Be careful not to mutate the reference
        /// target, as doing so may break assumptions the runtime makes.
        /// </summary>
        /// <typeparam name="T">Type of the parameter.</typeparam>
        /// <param name="item">The read-only reference to a valuetype of type T.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public static ref T AsRef<T>(in T item)
            where T : struct
        {
            return ref ILSupport.AsRef(in item);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static unsafe void CheckMemSwapOverlap(byte* dst, byte* src, long size)
        {
            if (dst + size > src && src + size > dst)
            {
                throw new InvalidOperationException("MemSwap memory blocks are overlapped.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckIndexRange(int index, int capacity)
        {
            if ((index > capacity - 1) || (index < 0))
            {
                throw new IndexOutOfRangeException(
                    $"Attempt to read or write from array index {index}, which is out of bounds. Array capacity is {capacity}. "
                    +"This may lead to a crash, data corruption, or reading invalid data."
                    );
            }
        }
    }
}
