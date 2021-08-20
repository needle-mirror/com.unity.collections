using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
#if !NET_DOTS
using System.Reflection;
#endif

namespace Unity.Collections
{
    /// <summary>
    /// For scheduling release of unmanaged resources.
    /// </summary>
    public interface INativeDisposable : IDisposable
    {
        /// <summary>
        /// Creates and schedules a job that will release all resources (memory and safety handles) of this collection.
        /// </summary>
        /// <param name="inputDeps">A job handle which the newly scheduled job will depend upon.</param>
        /// <returns>The handle of a new job that will release all resources (memory and safety handles) of this collection.</returns>
        JobHandle Dispose(JobHandle inputDeps);
    }

    /// <summary>
    /// Provides helper methods for collections.
    /// </summary>
    [BurstCompatible]
    public static class CollectionHelper
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void CheckAllocator(AllocatorManager.AllocatorHandle allocator)
        {
            if (!ShouldDeallocate(allocator))
                throw new ArgumentException($"Allocator {allocator} must not be None or Invalid");
        }

        /// <summary>
        /// The size in bytes of the current platform's L1 cache lines.
        /// </summary>
        /// <value>The size in bytes of the current platform's L1 cache lines.</value>
        public const int CacheLineSize = JobsUtility.CacheLineSize;

        [StructLayout(LayoutKind.Explicit)]
        internal struct LongDoubleUnion
        {
            [FieldOffset(0)]
            internal long longValue;

            [FieldOffset(0)]
            internal double doubleValue;
        }

        /// <summary>
        /// Returns the binary logarithm of the `value`, but the result is rounded down to the nearest integer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The binary logarithm of the `value`, but the result is rounded down to the nearest integer.</returns>
        public static int Log2Floor(int value)
        {
            return 31 - math.lzcnt((uint)value);
        }

        /// <summary>
        /// Returns the binary logarithm of the `value`, but the result is rounded up to the nearest integer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The binary logarithm of the `value`, but the result is rounded up to the nearest integer.</returns>
        public static int Log2Ceil(int value)
        {
            return 32 - math.lzcnt((uint)value - 1);
        }

        /// <summary>
        /// Returns an allocation size in bytes that factors in alignment.
        /// </summary>
        /// <example><code>
        /// // 55 aligned to 16 is 64.
        /// int size = CollectionHelper.Align(55, 16);
        /// </code></example>
        /// <param name="size">The size to align.</param>
        /// <param name="alignmentPowerOfTwo">A non-zero, positive power of two.</param>
        /// <returns>The smallest integer that is greater than or equal to `size` and is a multiple of `alignmentPowerOfTwo`.</returns>
        /// <exception cref="ArgumentException">Thrown if `alignmentPowerOfTwo` is not a non-zero, positive power of two.</exception>
        public static int Align(int size, int alignmentPowerOfTwo)
        {
            if (alignmentPowerOfTwo == 0)
                return size;

            CheckIntPositivePowerOfTwo(alignmentPowerOfTwo);

            return (size + alignmentPowerOfTwo - 1) & ~(alignmentPowerOfTwo - 1);
        }

        /// <summary>
        /// Returns an allocation size in bytes that factors in alignment.
        /// </summary>
        /// <example><code>
        /// // 55 aligned to 16 is 64.
        /// ulong size = CollectionHelper.Align(55, 16);
        /// </code></example>
        /// <param name="size">The size to align.</param>
        /// <param name="alignmentPowerOfTwo">A non-zero, positive power of two.</param>
        /// <returns>The smallest integer that is greater than or equal to `size` and is a multiple of `alignmentPowerOfTwo`.</returns>
        /// <exception cref="ArgumentException">Thrown if `alignmentPowerOfTwo` is not a non-zero, positive power of two.</exception>
        public static ulong Align(ulong size, ulong alignmentPowerOfTwo)
        {
            if (alignmentPowerOfTwo == 0)
                return size;

            CheckUlongPositivePowerOfTwo(alignmentPowerOfTwo);

            return (size + alignmentPowerOfTwo - 1) & ~(alignmentPowerOfTwo - 1);
        }

        /// <summary>
        /// Returns true if the address represented by the pointer has a given alignment.
        /// </summary>
        /// <param name="p">The pointer.</param>
        /// <param name="alignmentPowerOfTwo">A non-zero, positive power of two.</param>
        /// <returns>True if the address is a multiple of `alignmentPowerOfTwo`.</returns>
        /// <exception cref="ArgumentException">Thrown if `alignmentPowerOfTwo` is not a non-zero, positive power of two.</exception>
        public static unsafe bool IsAligned(void* p, int alignmentPowerOfTwo)
        {
            CheckIntPositivePowerOfTwo(alignmentPowerOfTwo);
            return ((ulong)p & ((ulong)alignmentPowerOfTwo - 1)) == 0;
        }

        /// <summary>
        /// Returns true if an offset has a given alignment.
        /// </summary>
        /// <param name="offset">An offset</param>
        /// <param name="alignmentPowerOfTwo">A non-zero, positive power of two.</param>
        /// <returns>True if the offset is a multiple of `alignmentPowerOfTwo`.</returns>
        /// <exception cref="ArgumentException">Thrown if `alignmentPowerOfTwo` is not a non-zero, positive power of two.</exception>
        public static bool IsAligned(ulong offset, int alignmentPowerOfTwo)
        {
            CheckIntPositivePowerOfTwo(alignmentPowerOfTwo);
            return (offset & ((ulong)alignmentPowerOfTwo - 1)) == 0;
        }

        /// <summary>
        /// Returns true if a positive value is a non-zero power of two.
        /// </summary>
        /// <remarks>Result is invalid if `value &lt; 0`.</remarks>
        /// <param name="value">A positive value.</param>
        /// <returns>True if the value is a non-zero, positive power of two.</returns>
        public static bool IsPowerOfTwo(int value)
        {
            return (value & (value - 1)) == 0;
        }

        /// <summary>
        /// Returns a (non-cryptographic) hash of a memory block.
        /// </summary>
        /// <remarks>The hash function used is [djb2](http://web.archive.org/web/20190508211657/http://www.cse.yorku.ca/~oz/hash.html).</remarks>
        /// <param name="ptr">A buffer.</param>
        /// <param name="bytes">The number of bytes to hash.</param>
        /// <returns>A hash of the bytes.</returns>
        public static unsafe uint Hash(void* ptr, int bytes)
        {
            // djb2 - Dan Bernstein hash function
            // http://web.archive.org/web/20190508211657/http://www.cse.yorku.ca/~oz/hash.html
            byte* str = (byte*)ptr;
            ulong hash = 5381;
            while (bytes > 0)
            {
                ulong c = str[--bytes];
                hash = ((hash << 5) + hash) + c;
            }
            return (uint)hash;
        }

        [NotBurstCompatible /* Used only for debugging. */]
        internal static void WriteLayout(Type type)
        {
#if !NET_DOTS
            Console.WriteLine($"   Offset | Bytes  | Name     Layout: {0}", type.Name);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                Console.WriteLine("   {0, 6} | {1, 6} | {2}"
                    , Marshal.OffsetOf(type, field.Name)
                    , Marshal.SizeOf(field.FieldType)
                    , field.Name
                );
            }
#else
            _ = type;
#endif
        }

        internal static bool ShouldDeallocate(AllocatorManager.AllocatorHandle allocator)
        {
            // Allocator.Invalid == container is not initialized.
            // Allocator.None    == container is initialized, but container doesn't own data.
            return allocator.ToAllocator > Allocator.None;
        }

        /// <summary>
        /// Tell Burst that an integer can be assumed to map to an always positive value.
        /// </summary>
        /// <param name="value">The integer that is always positive.</param>
        /// <returns>Returns `x`, but allows the compiler to assume it is always positive.</returns>
        [return: AssumeRange(0, int.MaxValue)]
        internal static int AssumePositive(int value)
        {
            return value;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [BurstDiscard] // Must use BurstDiscard because UnsafeUtility.IsUnmanaged is not burstable.
        [NotBurstCompatible  /* Used only for debugging. */]
        internal static void CheckIsUnmanaged<T>()
        {
            if (!UnsafeUtility.IsValidNativeContainerElementType<T>())
            {
                throw new ArgumentException($"{typeof(T)} used in native collection is not blittable, not primitive, or contains a type tagged as NativeContainer");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void CheckIntPositivePowerOfTwo(int value)
        {
            var valid = (value > 0) && ((value & (value - 1)) == 0);
            if (!valid)
            {
                throw new ArgumentException($"Alignment requested: {value} is not a non-zero, positive power of two.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void CheckUlongPositivePowerOfTwo(ulong value)
        {
            var valid = (value > 0) && ((value & (value - 1)) == 0);
            if (!valid)
            {
                throw new ArgumentException($"Alignment requested: {value} is not a non-zero, positive power of two.");
            }
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(AllocatorManager.AllocatorHandle) })]
        public static NativeArray<T> CreateNativeArray<T, U>(int length, ref U allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
            where T : struct
            where U : unmanaged, AllocatorManager.IAllocator
        {
            NativeArray<T> nativeArray;
            if (!allocator.IsCustomAllocator)
            {
                nativeArray = new NativeArray<T>(length, allocator.ToAllocator, options);
            }
            else
            {
                nativeArray = new NativeArray<T>();
                nativeArray.Initialize(length, ref allocator, options);
            }
            return nativeArray;
        }


        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        public static NativeArray<T> CreateNativeArray<T>(int length, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
            where T : struct
        {
            NativeArray<T> nativeArray;
            if(!AllocatorManager.IsCustomAllocator(allocator))
            {
                nativeArray = new NativeArray<T>(length, allocator.ToAllocator, options);
            }
            else
            {
                nativeArray = new NativeArray<T>();
                nativeArray.Initialize(length, allocator, options);
            }
            return nativeArray;
        }

        [NotBurstCompatible]
        public static NativeArray<T> CreateNativeArray<T>(NativeArray<T> array, AllocatorManager.AllocatorHandle allocator)
            where T : struct
        {
            NativeArray<T> nativeArray;
            if (!AllocatorManager.IsCustomAllocator(allocator))
            {
                nativeArray = new NativeArray<T>(array, allocator.ToAllocator);
            }
            else
            {
                nativeArray = new NativeArray<T>();
                nativeArray.Initialize(array.Length, allocator);
                nativeArray.CopyFrom(array);
            }
            return nativeArray;
        }

        [NotBurstCompatible]
        public static NativeArray<T> CreateNativeArray<T>(T[] array, AllocatorManager.AllocatorHandle allocator)
            where T : struct
        {
            NativeArray<T> nativeArray;
            if (!AllocatorManager.IsCustomAllocator(allocator))
            {
                nativeArray = new NativeArray<T>(array, allocator.ToAllocator);
            }
            else
            {
                nativeArray = new NativeArray<T>();
                nativeArray.Initialize(array.Length, allocator);
                nativeArray.CopyFrom(array);
            }
            return nativeArray;
        }

        [NotBurstCompatible]
        public static NativeArray<T> CreateNativeArray<T, U>(T[] array, ref U allocator)
            where T : struct
            where U : unmanaged, AllocatorManager.IAllocator
        {
            NativeArray<T> nativeArray;
            if (!allocator.IsCustomAllocator)
            {
                nativeArray = new NativeArray<T>(array, allocator.ToAllocator);
            }
            else
            {
                nativeArray = new NativeArray<T>();
                nativeArray.Initialize(array.Length, ref allocator);
                nativeArray.CopyFrom(array);
            }
            return nativeArray;
        }


        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int), typeof(AllocatorManager.AllocatorHandle) })]
        public static NativeMultiHashMap<TKey, TValue> CreateNativeMultiHashMap<TKey, TValue, U>(int length, ref U allocator)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
            where U : unmanaged, AllocatorManager.IAllocator
        {
            var nativeMultiHashMap = new NativeMultiHashMap<TKey, TValue>();
            nativeMultiHashMap.Initialize(length, ref allocator);
            return nativeMultiHashMap;
        }
    }
}
