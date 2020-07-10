using System;
using System.Diagnostics;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    ///
    /// </summary>
    public unsafe struct UnsafeScratchAllocator
    {
        void* m_Pointer;
        int m_LengthInBytes;
        readonly int m_CapacityInBytes;

        /// <summary>
        ///
        /// </summary>
        /// <param name="pointer"></param>
        /// <param name="capacityInBytes"></param>
        public UnsafeScratchAllocator(void* pointer, int capacityInBytes)
        {
            m_Pointer = pointer;
            m_LengthInBytes = 0;
            m_CapacityInBytes = capacityInBytes;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckAllocationDoesNotExceedCapacity(ulong requestedSize)
        {
            if (requestedSize > (ulong)m_CapacityInBytes)
                throw new ArgumentException($"Cannot allocate more than provided size in UnsafeScratchAllocator. Requested: {requestedSize} Size: {m_LengthInBytes} Capacity: {m_CapacityInBytes}");
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sizeInBytes"></param>
        /// <param name="alignmentInBytes"></param>
        /// <returns></returns>
        public void* Allocate(int sizeInBytes, int alignmentInBytes)
        {
            if (sizeInBytes == 0)
                return null;
            var alignmentMask = (ulong)(alignmentInBytes - 1);
            var end = (ulong)(IntPtr)m_Pointer + (ulong)m_LengthInBytes;
            end = (end + alignmentMask) & ~alignmentMask;
            var lengthInBytes = (byte*)(IntPtr)end - (byte*)m_Pointer;
            lengthInBytes += sizeInBytes;
            CheckAllocationDoesNotExceedCapacity((ulong)lengthInBytes);
            m_LengthInBytes = (int)lengthInBytes;
            return (void*)(IntPtr)end;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="count"></param>
        /// <returns></returns>
        public void* Allocate<T>(int count = 1) where T : struct
        {
            return Allocate(UnsafeUtility.SizeOf<T>() * count, UnsafeUtility.AlignOf<T>());
        }
    }
}
