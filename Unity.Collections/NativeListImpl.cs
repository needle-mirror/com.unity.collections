using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{

    /// <summary>
    /// What is this : struct that contains the data for a native list, that gets allocated using native memory allocation.
    /// Motivation(s): Need a single container struct to hold a native lists collection data.
    /// </summary>
	unsafe struct NativeListData
	{
		public void*                            buffer;
		public int								length;
		public int								capacity;
	}

    /// <summary>
    /// What is this : internal implementation of a variable size list, using native memory (not GC'd).
    /// Motivation(s): just need a resizable list that does not trigger the GC, for performance reasons.
    /// </summary>
    [StructLayout (LayoutKind.Sequential)]
#if ENABLE_UNITY_COLLECTIONS_CHECKS
    public unsafe struct NativeListImpl<T, TMemManager, TSentinel>
        where TSentinel : struct, INativeBufferSentinel
#else
	public unsafe struct NativeListImpl<T, TMemManager>
#endif
        where T : struct
        where TMemManager : struct, INativeBufferMemoryManager
	{
        public TMemManager m_MemoryAllocator;

	    [NativeDisableUnsafePtrRestriction]
	    NativeListData* m_ListData;

	    internal NativeListData* GetListData()
	    {
	        return m_ListData;
	    }

	    public void* RawBuffer => m_ListData;

	    public TMemManager Allocator => m_MemoryAllocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
	    internal TSentinel sentinel;
#endif

	    public T this [int index]
		{
			get
			{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if ((uint)index >= (uint)m_ListData->length)
                    throw new IndexOutOfRangeException($"Index {index} is out of range in NativeList of '{m_ListData->length}' Length.");
#endif

                return UnsafeUtility.ReadArrayElement<T>(m_ListData->buffer, index);
			}

			set
			{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			    if ((uint)index >= (uint)m_ListData->length)
                    throw new IndexOutOfRangeException($"Index {index} is out of range in NativeList of '{m_ListData->length}' Length.");
#endif

                UnsafeUtility.WriteArrayElement(m_ListData->buffer, index, value);
			}
		}

		public int Length
		{
			get
			{
				return m_ListData->length;
			}
		}

		public int Capacity
		{
			get
			{
			    if( m_ListData == null )
			        throw new NullReferenceException();
				return m_ListData->capacity;
			}

			set
			{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			    if (value < m_ListData->length)
			        throw new ArgumentException("Capacity must be larger than the length of the NativeList.");
#endif

				if (m_ListData->capacity == value)
					return;

				void* newData = UnsafeUtility.Malloc (value * UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), m_MemoryAllocator.Label);
				UnsafeUtility.MemCpy (newData, m_ListData->buffer, m_ListData->length * UnsafeUtility.SizeOf<T>());
				UnsafeUtility.Free (m_ListData->buffer, m_MemoryAllocator.Label);
			    m_ListData->buffer = newData;
			    m_ListData->capacity = value;
			}
		}

#if ENABLE_UNITY_COLLECTIONS_CHECKS
		public NativeListImpl(int capacity, Allocator allocatorLabel, TSentinel sentinel)
#else
		public NativeListImpl(int capacity, Allocator allocatorLabel)
#endif
		{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
		    this.sentinel = sentinel;
		    m_ListData = null;

		    if (!UnsafeUtility.IsBlittable<T>())
		    {
		        this.sentinel.Dispose();
		        throw new ArgumentException(string.Format("{0} used in NativeList<{0}> must be blittable", typeof(T)));
		    }
#endif
		    m_MemoryAllocator = default(TMemManager);
		    m_ListData = (NativeListData*)m_MemoryAllocator.Init( UnsafeUtility.SizeOf<NativeListData>(), UnsafeUtility.AlignOf<NativeListData>(), allocatorLabel );

			var elementSize = UnsafeUtility.SizeOf<T> ();

            //@TODO: Find out why this is needed?
            capacity = Math.Max(1, capacity);
		    m_ListData->buffer = UnsafeUtility.Malloc (capacity * elementSize, UnsafeUtility.AlignOf<T>(), allocatorLabel);

		    m_ListData->length = 0;
		    m_ListData->capacity = capacity;
		}

		public void Add(T element)
		{
			if (m_ListData->length >= m_ListData->capacity)
				Capacity = m_ListData->length + m_ListData->capacity * 2;

		    this[m_ListData->length++] = element;
		}

        //@TODO: Test for AddRange
        public void AddRange(NativeArray<T> elements)
        {
            if (m_ListData->length + elements.Length > m_ListData->capacity)
                Capacity = m_ListData->length + elements.Length * 2;

            var sizeOf = UnsafeUtility.SizeOf<T> ();
            UnsafeUtility.MemCpy((byte*)m_ListData->buffer + m_ListData->length * sizeOf, elements.GetUnsafePtr(), sizeOf * elements.Length);

            m_ListData->length += elements.Length;
        }

		public void RemoveAtSwapBack(int index)
		{
		    var newLength = m_ListData->length - 1;
			this[index] = this[newLength];
		    m_ListData->length = newLength;
		}

		public bool IsNull => m_ListData == null;

	    public void Dispose()
		{
		    if (m_ListData != null)
		    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
		        sentinel.Dispose();
#endif

		        UnsafeUtility.Free (m_ListData->buffer, m_MemoryAllocator.Label);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
		        m_ListData->buffer = (void*)0xDEADF00D;
#endif
		        m_MemoryAllocator.Dispose( m_ListData );
                m_ListData = null;
		    }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            else
		        throw new Exception("NativeList has yet to be allocated or has been dealocated!");
#endif
		}

		public void Clear()
		{
		    ResizeUninitialized (0);
		}

        /// <summary>
        /// Does NOT allocate memory, but shares it.
        /// </summary>
		public NativeArray<T> ToNativeArray()
		{
		    return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T> (m_ListData->buffer, m_ListData->length, Collections.Allocator.Invalid);
		}

		public void ResizeUninitialized(int length)
		{
		    Capacity = Math.Max(length, Capacity);
			m_ListData->length = length;
		}

#if ENABLE_UNITY_COLLECTIONS_CHECKS
	    public NativeListImpl<T, TMemManager, TSentinel> Clone()
	    {
	        var clone = new NativeListImpl<T, TMemManager, TSentinel>( Capacity, m_MemoryAllocator.Label, sentinel);
            UnsafeUtility.MemCpy(clone.m_ListData->buffer, m_ListData->buffer, m_ListData->length * UnsafeUtility.SizeOf<T>());
	        clone.m_ListData->length = m_ListData->length;

	        return clone;
	    }
#else
	    public NativeListImpl<T, TMemManager> Clone()
	    {
	        var clone = new NativeListImpl<T, TMemManager>(Capacity, m_MemoryAllocator.Label);
	        UnsafeUtility.MemCpy(clone.m_ListData->buffer, m_ListData->buffer, m_ListData->length * UnsafeUtility.SizeOf<T>());
	        clone.m_ListData->length = m_ListData->length;

	        return clone;
	    }
#endif

        public NativeArray<T> CopyToNativeArray(Allocator label)
	    {
	        var buffer = UnsafeUtility.Malloc( UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), label);
	        UnsafeUtility.MemCpy( buffer, m_ListData->buffer, Length * UnsafeUtility.SizeOf<T>());
	        var copy = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T> (buffer, Length, label);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
	        NativeArrayUnsafeUtility.SetAtomicSafetyHandle( ref copy, AtomicSafetyHandle.Create());
#endif
            return copy;
	    }
	}

}

