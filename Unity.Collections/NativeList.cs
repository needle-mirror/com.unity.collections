using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using System.Diagnostics;

namespace Unity.Collections
{
	[StructLayout (LayoutKind.Sequential)]
	[NativeContainer]
	[DebuggerDisplay("Length = {Length}")]
	[DebuggerTypeProxy(typeof(NativeListDebugView < >))]
	public struct NativeList<T> : IDisposable
        where T : struct
	{

#if ENABLE_UNITY_COLLECTIONS_CHECKS
	    internal NativeListImpl<T, DefaultMemoryManager, NativeBufferSentinel> m_Impl;
	    internal AtomicSafetyHandle m_Safety;
#else
	    internal NativeListImpl<T, DefaultMemoryManager> m_Impl;
#endif

        public unsafe NativeList(Allocator i_label) : this (1, i_label, 2) { }
	    public unsafe NativeList(int capacity, Allocator i_label) : this (capacity, i_label, 2) { }

	    unsafe NativeList(int capacity, Allocator i_label, int stackDepth)
	    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if UNITY_2018_3_OR_NEWER
	        var guardian = new NativeBufferSentinel(stackDepth, i_label);
	        m_Safety = (i_label == Allocator.Temp) ? AtomicSafetyHandle.GetTempMemoryHandle() : AtomicSafetyHandle.Create();
#else
	        var guardian = new NativeBufferSentinel(stackDepth);
	        m_Safety = AtomicSafetyHandle.Create();
#endif
	        m_Impl = new NativeListImpl<T, DefaultMemoryManager, NativeBufferSentinel>(capacity, i_label, guardian);
#else
            m_Impl = new NativeListImpl<T, DefaultMemoryManager>(capacity, i_label);
#endif
	    }

	    public T this [int index]
		{
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Impl[index];

            }
	        set
	        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
	            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
	            m_Impl[index] = value;

	        }
		}

	    public int Length
	    {
	        get
	        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
	            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
	            return m_Impl.Length;
	        }
	    }

	    public int Capacity
	    {
	        get
	        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
	            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
	            return m_Impl.Capacity;
	        }

	        set
	        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
	            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
	            m_Impl.Capacity = value;
	        }
	    }

		public void Add(T element)
		{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
		    AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
		    m_Impl.Add(element);
		}

        public void AddRange(NativeArray<T> elements)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif

            m_Impl.AddRange(elements);
        }

		public void RemoveAtSwapBack(int index)
		{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
		    AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);

            if( index < 0 || index >= Length )
                throw new ArgumentOutOfRangeException(index.ToString());
#endif
			m_Impl.RemoveAtSwapBack(index);
		}

		public bool IsCreated => !m_Impl.IsNull;

	    public void Dispose()
		{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
		    AtomicSafetyHandle.CheckDeallocateAndThrow(m_Safety);
#if UNITY_2018_3_OR_NEWER
		    if (AtomicSafetyHandle.IsTempMemoryHandle(m_Safety))
		        m_Safety = AtomicSafetyHandle.Create();
#endif
		    AtomicSafetyHandle.Release(m_Safety);
#endif
		    m_Impl.Dispose();
		}

		public void Clear()
		{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
		    AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif

		    m_Impl.Clear();
		}

	    public static implicit operator NativeArray<T> (NativeList<T> nativeList)
	    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
	        AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(nativeList.m_Safety);
	        var arraySafety = nativeList.m_Safety;
	        AtomicSafetyHandle.UseSecondaryVersion(ref arraySafety);
#endif

	        var array = nativeList.m_Impl.ToNativeArray();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
	        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, arraySafety);
#endif
	        return array;
	    }

	    public unsafe NativeArray<T> ToDeferredJobArray()
	    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
	        AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
#endif

	        byte* buffer = (byte*)m_Impl.GetListData();
	        // We use the first bit of the pointer to infer that the array is in list mode
	        // Thus the job scheduling code will need to patch it.
	        buffer += 1;
	        var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T> (buffer, 0, Allocator.Invalid);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
	        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
#endif

	        return array;
	    }


		public T[] ToArray()
		{
		    NativeArray<T> nativeArray = this;
		    return nativeArray.ToArray();
		}

		public void CopyFrom(T[] array)
		{
		    //@TODO: Thats not right... This doesn't perform a resize
		    Capacity = array.Length;
		    NativeArray<T> nativeArray = this;
		    nativeArray.CopyFrom(array);
		}

		public void ResizeUninitialized(int length)
		{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
		    AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
			m_Impl.ResizeUninitialized(length);
		}
	}


    sealed class NativeListDebugView<T> where T : struct
    {
        NativeList<T> m_Array;

        public NativeListDebugView(NativeList<T> array)
        {
            m_Array = array;
        }

        public T[] Items => m_Array.ToArray();
    }
}
namespace Unity.Collections.LowLevel.Unsafe
{
    public static class NativeListUnsafeUtility
    {
        public static unsafe void* GetUnsafePtr<T>(this NativeList<T> nativeList) where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(nativeList.m_Safety);
#endif
            var data = nativeList.m_Impl.GetListData();
            return data->buffer;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static AtomicSafetyHandle GetAtomicSafetyHandle<T>(ref NativeList<T> nativeList) where T : struct
        {
            return nativeList.m_Safety;
        }
#endif

        public static unsafe void* GetInternalListDataPtrUnchecked<T>(ref NativeList<T> nativeList) where T : struct
        {
            return nativeList.m_Impl.GetListData();
        }
    }
}
