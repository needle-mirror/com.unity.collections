using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections.Experimental
{
    internal unsafe struct StructLayoutData4
    {
        public const int ChunkSizeBytes = 32;
        public const int ElementSize = 4;
        public const int ElementsPerChunk = ChunkSizeBytes / ElementSize;

        internal struct FieldInfo
        {
            public int Offset;
            //public int Size;
        }

        public bool IsCreated => m_Fields != null;
        public int FieldCount => m_Fields.Length;

        private FieldInfo[] m_Fields;

        public StructLayoutData4(Type t)
        {
            m_Fields = ComputeFieldInfo(t);
        }

        /// <summary>
        /// Compute how many chunks of ChunkSizeBytes are needed to store count elements of data.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public int ChunksNeeded(int count)
        {
            return (count + ElementsPerChunk - 1) >> 3;
        }

        public int ChunkIndex(int element)
        {
            return element >> 3;
        }

        public int ChunkOffset(int element)
        {
            return element & (ElementsPerChunk - 1);
        }

        public FieldInfo GetFieldInfo(int attrIndex)
        {
            return m_Fields[attrIndex];
        }

        private static FieldInfo[] ComputeFieldInfo(Type t)
        {
            var result = new List<FieldInfo>();
            FindFields(result, t, 0);
            return result.ToArray();
        }

        private static void FindFields(List<FieldInfo> result, Type type, int parentOffset)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
            {
                int offset = parentOffset + UnsafeUtility.GetFieldOffset(field);

                if (field.FieldType.IsPrimitive || field.FieldType.IsPointer)
                {
                    int sizeOf = -1;

                    if (field.FieldType.IsPointer)
                        sizeOf = UnsafeUtility.SizeOf<IntPtr>();
                    else 
                        sizeOf = UnsafeUtility.SizeOf(field.FieldType);

                    if ((sizeOf & (sizeOf - 1)) != 0)
                    {
                        throw new ArgumentException($"Field {type}.{field} is of size {sizeOf} which is not a power of two");
                    }

                    if (sizeOf != ElementSize)
                    {
                        throw new ArgumentException($"Field {type}.{field} is of size {sizeOf}; currently only types of size {ElementSize} bytes are allowed");
                    }

                    result.Add(new FieldInfo { Offset = offset /*, Size = sizeOf */ });
                }
                else
                {
                    FindFields(result, field.FieldType, offset);
                }
            }
        }
    }

    public unsafe struct NativeArraySOA<T> : IDisposable where T : struct
    {
        private static StructLayoutData4 ms_CachedLayout;

        private byte* m_Base;
        private int   m_Length;
        private Allocator m_Allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
		internal AtomicSafetyHandle 	m_Safety;
		[NativeSetClassTypeToNullOnSchedule]
		DisposeSentinel					m_DisposeSentinel;
#endif

        public int Length => m_Length;
        public Allocator Allocator => m_Allocator;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckReadAccess(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            if ((uint)index >= (uint)m_Length)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range in NativeArraySOA of '{1}' Length.", index, m_Length));
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckWriteAccess(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            if ((uint)index >= (uint)m_Length)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range in NativeArraySOA of '{1}' Length.", index, m_Length));
#endif
        }

        public NativeArraySOA(int length, Allocator label)
            : this(length, label, 1)
        {}

        public NativeArraySOA(int length, Allocator label, int stackDepth)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!UnsafeUtility.IsBlittable<T>())
            {
                throw new ArgumentException($"{typeof(T)} is not blittable");
            }
#endif
            if (!ms_CachedLayout.IsCreated)
            {
                ms_CachedLayout = new StructLayoutData4(typeof(T));
            }

            m_Base = (byte*) UnsafeUtility.Malloc(StructLayoutData4.ChunkSizeBytes * ms_CachedLayout.ChunksNeeded(length), StructLayoutData4.ChunkSizeBytes, Allocator.Temp);
            m_Length = length;
            m_Allocator = label;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, stackDepth);
#endif
        }

        public void Dispose()
        {
			#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(m_Safety, ref m_DisposeSentinel);
			#endif

            if (m_Base != null)
            {
                UnsafeUtility.Free(m_Base, m_Allocator);
            }
        }

        public T this [int index]
        {
            get
            {
                CheckReadAccess(index);

                T result = default(T);

                int chunkIndex = ms_CachedLayout.ChunkIndex(index);
                int chunkOffset = ms_CachedLayout.ChunkOffset(index);

                int fieldCount = ms_CachedLayout.FieldCount;

                uint* bp = (uint*) (m_Base + StructLayoutData4.ChunkSizeBytes * ms_CachedLayout.FieldCount * chunkIndex + StructLayoutData4.ElementSize * chunkOffset);
                uint* target = (uint*) UnsafeUtility.AddressOf(ref result);

                for (int field = 0; field < fieldCount; ++field)
                {
                    var fieldInfo = ms_CachedLayout.GetFieldInfo(field);
                    target[fieldInfo.Offset / 4] = *bp;
                    bp += StructLayoutData4.ElementsPerChunk;
                }

                return result;
            }

            set
            {
                CheckWriteAccess(index);

                int chunkIndex = ms_CachedLayout.ChunkIndex(index);
                int chunkOffset = ms_CachedLayout.ChunkOffset(index);

                int fieldCount = ms_CachedLayout.FieldCount;

                uint* bp = (uint*) UnsafeUtility.AddressOf(ref value);
                uint* target = (uint*) (m_Base + StructLayoutData4.ChunkSizeBytes * ms_CachedLayout.FieldCount * chunkIndex + StructLayoutData4.ElementSize * chunkOffset);

                for (int field = 0; field < fieldCount; ++field)
                {
                    var fieldInfo = ms_CachedLayout.GetFieldInfo(field);
                    *target = bp[fieldInfo.Offset / 4];
                    target += StructLayoutData4.ElementsPerChunk;
                }
            }
        }
    }
}
