using System;
using Unity.Collections.LowLevel.Unsafe;
using System.Diagnostics;

namespace Unity.Collections
{
    // this is a fixed 64 byte buffer, whose interface is a resizable array of T.
    // for times when you need a struct member that is a small but resizable array of T,
    // but you don't want to visit the heap or do everything by hand with naked primitives.
    public unsafe struct ResizableArray64Byte<T> where T : struct
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private unsafe void CheckElementAccess(int index)
        {
            if (index < 0 || index >= Length)
                throw new IndexOutOfRangeException($"Index {index} is out of range of '{Length}' Length.");
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private unsafe void CheckResize(int newCount)
        {
            if (newCount < 0 || newCount > Capacity)
                throw new IndexOutOfRangeException($"NewCount {newCount} is out of range of '{Capacity}' Capacity.");
        }
        
        [WriteAccessRequired] public void* GetUnsafePointer()
        {
            fixed (void* b = m_buffer)
                return b;
        }

        private const int totalSizeInBytes = 64;
        private const int bufferSizeInBytes = totalSizeInBytes - sizeof(int);
        private const int bufferSizeInInts = bufferSizeInBytes / sizeof(int);
        
        int m_count;
        private fixed int m_buffer[bufferSizeInInts];
        
        public int Length
        {
            get => m_count;                
            [WriteAccessRequired] set
            {
                CheckResize(value);
                m_count = value;
            }
        }

        public int Capacity
        {
            get => bufferSizeInBytes / UnsafeUtility.SizeOf<T>();
        }
        
        public T this[int index]
        {
            get
            {
                CheckElementAccess(index);
                fixed(void *b = m_buffer)
                    return UnsafeUtility.ReadArrayElement<T>(b, index);
            }
            [WriteAccessRequired] set
            {
                CheckElementAccess(index);
                fixed(void *b = m_buffer)
                    UnsafeUtility.WriteArrayElement<T>(b, index, value);
            }
        }

        [WriteAccessRequired] public void Add(T a)
        {
            this[Length++] = a;
        }        

        public ResizableArray64Byte(T a)
        {
            m_count = 1;
            this[0] = a;
        }        
        public ResizableArray64Byte(T a, T b)
        {
            m_count = 2;
            this[0] = a;
            this[1] = b;
        }
        public ResizableArray64Byte(T a, T b, T c)
        {
            m_count = 3;
            this[0] = a;
            this[1] = b;
            this[2] = c;
        }
        public ResizableArray64Byte(T a, T b, T c, T d)
        {
            m_count = 4;
            this[0] = a;
            this[1] = b;
            this[2] = c;
            this[3] = d;
        }
        public ResizableArray64Byte(T a, T b, T c, T d, T e)
        {
            m_count = 5;
            this[0] = a;
            this[1] = b;
            this[2] = c;
            this[3] = d;
            this[4] = e;
        }

    }
}
