using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    /// <summary>
    /// What is this : required interface for memory management implementations for Native Buffers (NativeList, NativeQueue, etc)
    /// Motivation(s): need to provide generic native buffer classes with injectable logic as to how memory is allocated/dealocated.
    ///     by sperating the container from the memory manager, different schemes can be used (ref counting for example) by the
    ///     generic container struct.
    /// </summary>
    public interface INativeBufferMemoryManager
    {
        unsafe void* Init(int size, int aligment, Allocator allocatorLabel);
        Allocator Label { get; }
        unsafe void Dispose(void * buffer);
    }

    /// <summary>
    /// What is this : Default memory manager for native buffers, that does nothing special.
    /// Motivation(s): NativeList and co. require a plain vanila memory manager. this is it.
    /// </summary>
    public unsafe struct DefaultMemoryManager : INativeBufferMemoryManager
    {
        public Allocator Label { get; private set; }

        public void Dispose(void * buffer)
        {
            UnsafeUtility.Free(buffer, Label);
        }

        public void * Init(int size, int aligment, Allocator allocatorLabel)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if( allocatorLabel == Allocator.Invalid )
                throw new ArgumentException("Invalid allocator label provided");
#endif

            Label = allocatorLabel;

            var buffer = UnsafeUtility.Malloc( size, aligment, allocatorLabel);
            return buffer;
        }
    }

#if ENABLE_UNITY_COLLECTIONS_CHECKS

    /// <summary>
    /// What is this : interface for types of Sentinels used by NativeBuffers.
    /// Motivation(s): NativeBuffers support defferent memory management schemes (NativeList vs NativeListField for example)
    ///  and some schemes require a Sentinel to detect and report memory leaks while others don't, But the common implementation
    ///  struct (for example, NativeListImpl) must deal with any type of sentinel. this interface allows it to do that.
    /// </summary>
    public interface INativeBufferSentinel : IDisposable
    {
    }

    /// <summary>
    /// What is this : this implementation offers an automatic notification of leaks by incorporating a DisposeSentinel.
    /// Motivation(s): native buffers that do not use ref counting are easy to leak and we need a way to inform users of when and where.
    /// </summary>
    public struct NativeBufferSentinel : INativeBufferSentinel
    {
        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;

#if UNITY_2018_3_OR_NEWER
        public NativeBufferSentinel(int stackDepth, Allocator allocator)
        {
            AtomicSafetyHandle tempASH;
            DisposeSentinel.Create(out tempASH, out m_DisposeSentinel, stackDepth, allocator);
            if (allocator != Allocator.Temp)
                AtomicSafetyHandle.Release(tempASH);
        }
#else
        public NativeBufferSentinel(int stackDepth)
        {
            AtomicSafetyHandle tempASH;
            DisposeSentinel.Create(out tempASH, out m_DisposeSentinel, stackDepth);
            AtomicSafetyHandle.Release(tempASH);
        }
#endif

        public void Dispose()
        {
            DisposeSentinel.Clear(ref m_DisposeSentinel);
        }
    }

    /// <summary>
    /// What is this: for native buffers that dont need memory sentinels, this implementation is just an empty husk.
    /// </summary>
    public struct NativeBufferFakeSentinel : INativeBufferSentinel
    {
        public void Dispose() { }
    }

#endif

}
