using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    // this is a fixed 64 byte buffer, whose interface is a resizable array of T.
    // for times when you need a struct member that is a small but resizable array of T,
    // but you don't want to visit the heap or do everything by hand with naked primitives.
    [PublicAPI]
    public unsafe struct ResizableArray64Byte<T> where T : struct
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckElementAccess(int index)
        {
            if (index < 0 || index >= Length)
                throw new IndexOutOfRangeException($"Index {index} is out of range of '{Length}' Length.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckResize(int newCount)
        {
            if (newCount < 0 || newCount > Capacity)
                throw new IndexOutOfRangeException($"NewCount {newCount} is out of range of '{Capacity}' Capacity.");
        }

        [WriteAccessRequired] public void* GetUnsafePointer()
        {
            fixed (void* b = m_Buffer)
                return b;
        }

        const int k_TotalSizeInBytes = 64;
        const int k_BufferSizeInBytes = k_TotalSizeInBytes - sizeof(int);
        const int k_BufferSizeInInts = k_BufferSizeInBytes / sizeof(int);

        int m_Count;
#pragma warning disable 649
        fixed int m_Buffer[k_BufferSizeInInts];
#pragma warning restore 649

        public int Length
        {
            get => m_Count;
            [WriteAccessRequired] set
            {
                CheckResize(value);
                m_Count = value;
            }
        }

        public int Capacity
            => k_BufferSizeInBytes / UnsafeUtility.SizeOf<T>();

        public T this[int index]
        {
            get
            {
                CheckElementAccess(index);
                fixed(void *b = m_Buffer)
                    return UnsafeUtility.ReadArrayElement<T>(b, index);
            }
            [WriteAccessRequired] set
            {
                CheckElementAccess(index);
                fixed(void *b = m_Buffer)
                    UnsafeUtility.WriteArrayElement(b, index, value);
            }
        }
        
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        public override int GetHashCode()
            => (int)CollectionHelper.hash(GetUnsafePointer(), m_Count * UnsafeUtility.SizeOf<T>());

        [WriteAccessRequired] public void Add(T a)
        {
            this[Length++] = a;
        }

        public ResizableArray64Byte(T a)
        {
            m_Count = 1;
            CheckResize(1);
            this[0] = a;
        }

        public ResizableArray64Byte(T a, T b)
        {
            m_Count = 2;
            CheckResize(2);
            this[0] = a;
            this[1] = b;
        }

        public ResizableArray64Byte(T a, T b, T c)
        {
            m_Count = 3;
            CheckResize(3);
            this[0] = a;
            this[1] = b;
            this[2] = c;
        }

        public ResizableArray64Byte(T a, T b, T c, T d)
        {
            m_Count = 4;
            CheckResize(4);
            this[0] = a;
            this[1] = b;
            this[2] = c;
            this[3] = d;
        }

        public ResizableArray64Byte(T a, T b, T c, T d, T e)
        {
            m_Count = 5;
            CheckResize(5);
            this[0] = a;
            this[1] = b;
            this[2] = c;
            this[3] = d;
            this[4] = e;
        }

    }
}
