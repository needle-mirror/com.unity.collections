using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.Collections
{
    /// <summary>
    /// Arbitrary sized array of bits.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Length = {Length}, IsCreated = {IsCreated}")]
    public unsafe struct NativeBitArray
        : INativeDisposable
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeBitArray>();

        [BurstDiscard]
        static void CreateStaticSafetyId()
        {
            s_staticSafetyId.Data = AtomicSafetyHandle.NewStaticSafetyId<NativeBitArray>();
        }

        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeBitArray m_BitArray;

        /// <summary>
        /// Constructs a new container with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="numBits">Number of bits.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        public NativeBitArray(int numBits, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
            : this(numBits, allocator, options, 2)
        {
        }

        NativeBitArray(int numBits, Allocator allocator, NativeArrayOptions options, int disposeSentinelStackDepth)
        {
            CheckAllocator(allocator);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, disposeSentinelStackDepth, allocator);
            if (s_staticSafetyId.Data == 0)
            {
                CreateStaticSafetyId();
            }
            AtomicSafetyHandle.SetStaticSafetyId(ref m_Safety, s_staticSafetyId.Data);
#endif
            m_BitArray = new UnsafeBitArray(numBits, allocator, options);
        }

        /// <summary>
        /// Reports whether memory for the container is allocated.
        /// </summary>
        /// <value>True if this container object's internal storage has been allocated.</value>
        /// <remarks>Note that the container storage is not created if you use the default constructor. You must specify
        /// at least an allocation type to construct a usable container.</remarks>
        public bool IsCreated => m_BitArray.IsCreated;

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

            m_BitArray.Dispose();
        }

        /// <summary>
        /// Safely disposes of this container and deallocates its memory when the jobs that use it have completed.
        /// </summary>
        /// <remarks>You can call this function dispose of the container immediately after scheduling the job. Pass
        /// the [JobHandle](https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.html) returned by
        /// the [Job.Schedule](https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJobExtensions.Schedule.html)
        /// method using the `jobHandle` parameter so the job scheduler can dispose the container after all jobs
        /// using it have run.</remarks>
        /// <param name="inputDeps">The job handle or handles for any scheduled jobs that use this container.</param>
        /// <returns>A new job handle containing the prior handles as well as the handle for the job that deletes
        /// the container.</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Clear(ref m_DisposeSentinel);
#endif
            var jobHandle = m_BitArray.Dispose(inputDeps);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(m_Safety);
#endif

            return jobHandle;
        }

        /// <summary>
        /// Number of bits.
        /// </summary>
        /// <value>The number of bits.</value>
        public int Length
        {
            get
            {
                CheckRead();
                return CollectionHelper.AssumePositive(m_BitArray.Length);
            }
        }

        /// <summary>
        /// Clear all bits to 0.
        /// </summary>
        public void Clear()
        {
            CheckWrite();
            m_BitArray.Clear();
        }

        /// <summary>
        /// Return a native array that aliases the original bit array contents.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the container.</typeparam>
        /// <exception cref="InvalidOperationException">Thrown if output size doesn't match input, or if reinterpreted data would be truncated.</exception>
        /// <returns>Native array view into bit array.</returns>
        public NativeArray<T> AsNativeArray<T>() where T : unmanaged
        {
            CheckReadBounds<T>();

            var bitsPerElement = UnsafeUtility.SizeOf<T>() * 8;
            var length = m_BitArray.Length / bitsPerElement;

            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(m_BitArray.Ptr, length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.UseSecondaryVersion(ref m_Safety);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
#endif
            return array;
        }

        /// <summary>
        /// Set single bit to desired boolean value.
        /// </summary>
        /// <param name="pos">Position in bit array.</param>
        /// <param name="value">Value of bits to set.</param>
        public void Set(int pos, bool value)
        {
            CheckWrite();
            m_BitArray.Set(pos, value);
        }

        /// <summary>
        /// Set bits to desired boolean value.
        /// </summary>
        /// <param name="pos">Position in bit array.</param>
        /// <param name="value">Value of bits to set.</param>
        /// <param name="numBits">Number of bits to set.</param>
        public void SetBits(int pos, bool value, int numBits)
        {
            CheckWrite();
            m_BitArray.SetBits(pos, value, numBits);
        }

        /// <summary>
        /// Sets bits in range as ulong.
        /// </summary>
        /// <param name="pos">Position in bit array.</param>
        /// <param name="value">Value of bits to set.</param>
        /// <param name="numBits">Number of bits to set (must be 1-64).</param>
        public void SetBits(int pos, ulong value, int numBits = 1)
        {
            CheckWrite();
            m_BitArray.SetBits(pos, value, numBits);
        }

        /// <summary>
        /// Returns all bits in range as ulong.
        /// </summary>
        /// <param name="pos">Position in bit array.</param>
        /// <param name="numBits">Number of bits to get (must be 1-64).</param>
        /// <returns>Returns requested range of bits.</returns>
        public ulong GetBits(int pos, int numBits = 1)
        {
            CheckRead();
            return m_BitArray.GetBits(pos, numBits);
        }

        /// <summary>
        /// Returns true is bit at position is set.
        /// </summary>
        /// <param name="pos">Position in bit array.</param>
        /// <returns>Returns true if bit is set.</returns>
        public bool IsSet(int pos)
        {
            CheckRead();
            return m_BitArray.IsSet(pos);
        }

        /// <summary>
        /// Copy block of bits from source to destination.
        /// </summary>
        /// <param name="dstPos">Destination position in bit array.</param>
        /// <param name="srcPos">Source position in bit array.</param>
        /// <param name="numBits">Number of bits to copy.</param>
        public void Copy(int dstPos, int srcPos, int numBits)
        {
            CheckWrite();
            m_BitArray.Copy(dstPos, srcPos, numBits);
        }

        /// <summary>
        /// Copy block of bits from source to destination.
        /// </summary>
        /// <param name="dstPos">Destination position in bit array.</param>
        /// <param name="srcBitArray">Source bit array from which bits will be copied.</param>
        /// <param name="srcPos">Source position in bit array.</param>
        /// <param name="numBits">Number of bits to copy.</param>
        public void Copy(int dstPos, ref NativeBitArray srcBitArray, int srcPos, int numBits)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(srcBitArray.m_Safety);
#endif
            CheckWrite();
            m_BitArray.Copy(dstPos, ref srcBitArray.m_BitArray, srcPos, numBits);
        }

        /// <summary>
        /// Returns true if none of bits in range are set.
        /// </summary>
        /// <param name="pos">Position in bit array.</param>
        /// <param name="numBits">Number of bits to test.</param>
        /// <returns>Returns true if none of bits are set.</returns>
        public bool TestNone(int pos, int numBits = 1)
        {
            CheckRead();
            return m_BitArray.TestNone(pos, numBits);
        }

        /// <summary>
        /// Returns true if any of bits in range are set.
        /// </summary>
        /// <param name="pos">Position in bit array.</param>
        /// <param name="numBits">Number of bits to test.</param>
        /// <returns>Returns true if at least one bit is set.</returns>
        public bool TestAny(int pos, int numBits = 1)
        {
            CheckRead();
            return m_BitArray.TestAny(pos, numBits);
        }

        /// <summary>
        /// Returns true if all of bits in range are set.
        /// </summary>
        /// <param name="pos">Position in bit array.</param>
        /// <param name="numBits">Number of bits to test.</param>
        /// <returns>Returns true if all bits are set.</returns>
        public bool TestAll(int pos, int numBits = 1)
        {
            CheckRead();
            return m_BitArray.TestAll(pos, numBits);
        }

        /// <summary>
        /// Calculate number of set bits.
        /// </summary>
        /// <param name="pos">Position in bit array.</param>
        /// <param name="numBits">Number of bits to perform count.</param>
        /// <returns>Number of set bits.</returns>
        public int CountBits(int pos, int numBits = 1)
        {
            CheckRead();
            return m_BitArray.CountBits(pos, numBits);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckAllocator(Allocator allocator)
        {
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckReadBounds<T>() where T : unmanaged
        {
            CheckRead();

            var bitsPerElement = UnsafeUtility.SizeOf<T>() * 8;
            var length = m_BitArray.Length / bitsPerElement;

            if (length == 0)
            {
                throw new InvalidOperationException($"Number of bits in the NativeBitArray {m_BitArray.Length} is not sufficient to cast to NativeArray<T> {UnsafeUtility.SizeOf<T>() * 8}.");
            }
            else if (m_BitArray.Length != bitsPerElement* length)
            {
                throw new InvalidOperationException($"Number of bits in the NativeBitArray {m_BitArray.Length} couldn't hold multiple of T {UnsafeUtility.SizeOf<T>()}. Output array would be truncated.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        }
    }
}

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// NativeBitArray unsafe utility helpers.
    /// </summary>
    public static class NativeBitArrayUnsafeUtility
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        /// <summary>
        /// Retrieve container's atomic safety handle.
        /// </summary>
        /// <param name="container"></param>
        /// <returns>Container's atomic safety handle.</returns>
        public static AtomicSafetyHandle GetAtomicSafetyHandle(in NativeBitArray container)
        {
            return container.m_Safety;
        }

        /// <summary>
        /// Set container's atomic safety handle.
        /// </summary>
        /// <param name="container">Containter to set atomic safety handle on.</param>
        /// <param name="safety">Atomic safety handle.</param>
        public static void SetAtomicSafetyHandle(ref NativeBitArray container, AtomicSafetyHandle safety)
        {
            container.m_Safety = safety;
        }
#endif

        /// <summary>
        /// Convert existing data to bit array container.
        /// </summary>
        /// <param name="ptr">Pointer to data.</param>
        /// <param name="sizeInBytes">Size of data in bytes. Must be multiple of 8-bytes.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns>Returns bit array container.</returns>
        public static unsafe NativeBitArray ConvertExistingDataToNativeBitArray(void* ptr, int sizeInBytes, Allocator allocator)
        {
            return new NativeBitArray
            {
                m_BitArray = new UnsafeBitArray(ptr, sizeInBytes, allocator),
            };
        }
    }
}
