using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.Collections
{
    /// <summary>
    /// Iterator.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the container.</typeparam>
    public struct NativeMultiHashMapIterator<TKey>
        where TKey : struct
    {
        internal TKey key;
        internal int NextEntryIndex;
        internal int EntryIndex;

        /// <summary>
        /// Returns entry index.
        /// </summary>
        /// <returns>Entry index.</returns>
        public int GetEntryIndex() => EntryIndex;
    }

    /// <summary>
    /// Unordered associative array, a collection of keys and values. This container can store multiple values for every key.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the container.</typeparam>
    /// <typeparam name="TValue">The type of the values in the container.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerTypeProxy(typeof(NativeMultiHashMapDebuggerTypeProxy<,>))]
    public unsafe struct NativeMultiHashMap<TKey, TValue>
        : INativeDisposable
        , IEnumerable<KeyValue<TKey, TValue>> // Used by collection initializers.
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        internal UnsafeMultiHashMap<TKey, TValue> m_MultiHashMapData;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeMultiHashMap<TKey, TValue>>();

        [BurstDiscard]
        static void CreateStaticSafetyId()
        {
            s_staticSafetyId.Data = AtomicSafetyHandle.NewStaticSafetyId<NativeMultiHashMap<TKey, TValue>>();
        }

        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif

        /// <summary>
        /// Constructs a new container with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="capacity">The initial capacity of the container. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        public NativeMultiHashMap(int capacity, Allocator allocator)
            : this(capacity, allocator, 2)
        {
        }

        NativeMultiHashMap(int capacity, Allocator allocator, int disposeSentinelStackDepth)
        {
            m_MultiHashMapData = new UnsafeMultiHashMap<TKey, TValue>(capacity, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, disposeSentinelStackDepth, allocator);

            if (s_staticSafetyId.Data == 0)
            {
                CreateStaticSafetyId();
            }
            AtomicSafetyHandle.SetStaticSafetyId(ref m_Safety, s_staticSafetyId.Data);
#endif
        }

        /// <summary>
        /// Reports whether container is empty.
        /// </summary>
        /// <value>True if this container empty.</value>
        public bool IsEmpty
        {
            get
            {
                CheckRead();
                return m_MultiHashMapData.IsEmpty;
            }
        }

        /// <summary>
        /// The current number of items in the container.
        /// </summary>
        /// <returns>The item count.</returns>
        public int Count()
        {
            CheckRead();
            return m_MultiHashMapData.Count();
        }

        /// <summary>
        /// The number of items that can fit in the container.
        /// </summary>
        /// <value>The number of items that the container can hold before it resizes its internal storage.</value>
        /// <remarks>Capacity specifies the number of items the container can currently hold. You can change Capacity
        /// to fit more or fewer items. Changing Capacity creates a new array of the specified size, copies the
        /// old array to the new one, and then deallocates the original array memory.</remarks>
        public int Capacity
        {
            get
            {
                CheckRead();
                return m_MultiHashMapData.Capacity;
            }

            set
            {
                CheckWrite();
                m_MultiHashMapData.Capacity = value;
            }
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        /// <remarks>Containers capacity remains unchanged.</remarks>
        public void Clear()
        {
            CheckWrite();
            m_MultiHashMapData.Clear();
        }

        /// <summary>
        /// Add an element with the specified key and value into the container. If the key already exist an ArgumentException will be thrown.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="item">The value of the element to add.</param>
        public void Add(TKey key, TValue item)
        {
            CheckWrite();
            m_MultiHashMapData.Add(key, item);
        }

        /// <summary>
        /// Removes all elements with the specified key from the container.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>Returns number of removed items.</returns>
        public int Remove(TKey key)
        {
            CheckWrite();
            return m_MultiHashMapData.Remove(key);
        }

        /// <summary>
        /// Removes all elements with the specified iterator the container.
        /// </summary>
        /// <param name="it">Iterator pointing at value to remove.</param>
        public void Remove(NativeMultiHashMapIterator<TKey> it)
        {
            CheckWrite();
            m_MultiHashMapData.Remove(it);
        }

        /// <summary>
        /// Retrieve iterator for the first value for the key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="item">Output value.</param>
        /// <param name="it">Iterator.</param>
        /// <returns>Returns true if the container contains the key.</returns>
        public bool TryGetFirstValue(TKey key, out TValue item, out NativeMultiHashMapIterator<TKey> it)
        {
            CheckRead();
            return m_MultiHashMapData.TryGetFirstValue(key, out item, out it);
        }

        /// <summary>
        /// Retrieve iterator to the next value for the key.
        /// </summary>
        /// <param name="item">Output value.</param>
        /// <param name="it">Iterator.</param>
        /// <returns>Returns true if next value for the key is found.</returns>
        public bool TryGetNextValue(out TValue item, ref NativeMultiHashMapIterator<TKey> it)
        {
            CheckRead();
            return m_MultiHashMapData.TryGetNextValue(out item, ref it);
        }

        /// <summary>
        /// Determines whether an key is in the container.
        /// </summary>
        /// <param name="key">The key to locate in the container.</param>
        /// <returns>Returns true if the container contains the key.</returns>
        public bool ContainsKey(TKey key)
        {
            return TryGetFirstValue(key, out var temp0, out var temp1);
        }

        /// <summary>
        /// Count number of values for specified key.
        /// </summary>
        /// <param name="key">The key to locate in the container.</param>
        /// <returns></returns>
        public int CountValuesForKey(TKey key)
        {
            if (!TryGetFirstValue(key, out var value, out var iterator))
            {
                return 0;
            }

            var count = 1;
            while (TryGetNextValue(out value, ref iterator))
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// Replace value at iterator.
        /// </summary>
        /// <param name="item">Value.</param>
        /// <param name="it">Iterator</param>
        /// <returns>Returns true if value was sucessfuly replaced.</returns>
        public bool SetValue(TValue item, NativeMultiHashMapIterator<TKey> it)
        {
            CheckWrite();
            return m_MultiHashMapData.SetValue(item, it);
        }

        /// <summary>
        /// Reports whether memory for the container is allocated.
        /// </summary>
        /// <value>True if this container object's internal storage has been allocated.</value>
        /// <remarks>Note that the container storage is not created if you use the default constructor. You must specify
        /// at least an allocation type to construct a usable container.</remarks>
        public bool IsCreated => m_MultiHashMapData.IsCreated;

        /// <summary>
        /// Disposes of this multi-hashmap and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            m_MultiHashMapData.Dispose();
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
            // [DeallocateOnJobCompletion] is not supported, but we want the deallocation
            // to happen in a thread. DisposeSentinel needs to be cleared on main thread.
            // AtomicSafetyHandle can be destroyed after the job was scheduled (Job scheduling
            // will check that no jobs are writing to the container).
            DisposeSentinel.Clear(ref m_DisposeSentinel);

            var jobHandle = new UnsafeHashMapDataDisposeJob { Data = new UnsafeHashMapDataDispose { m_Buffer = m_MultiHashMapData.m_Buffer, m_AllocatorLabel = m_MultiHashMapData.m_AllocatorLabel, m_Safety = m_Safety } }.Schedule(inputDeps);

            AtomicSafetyHandle.Release(m_Safety);
#else
            var jobHandle = new UnsafeHashMapDataDisposeJob { Data = new UnsafeHashMapDataDispose { m_Buffer = m_MultiHashMapData.m_Buffer, m_AllocatorLabel = m_MultiHashMapData.m_AllocatorLabel } }.Schedule(inputDeps);
#endif
            m_MultiHashMapData.m_Buffer = null;

            return jobHandle;
        }

        /// <summary>
        /// Returns array populated with keys.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns>Array of keys.</returns>
        public NativeArray<TKey> GetKeyArray(Allocator allocator)
        {
            CheckRead();
            return m_MultiHashMapData.GetKeyArray(allocator);
        }

        /// <summary>
        /// Returns array populated with values.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns>Array of values.</returns>
        public NativeArray<TValue> GetValueArray(Allocator allocator)
        {
            CheckRead();
            return m_MultiHashMapData.GetValueArray(allocator);
        }

        /// <summary>
        /// Returns arrays populated with keys and values.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns>Array of keys-values.</returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(Allocator allocator)
        {
            CheckRead();
            return m_MultiHashMapData.GetKeyValueArrays(allocator);
        }

        /// <summary>
        /// Returns parallel writer instance.
        /// </summary>
        /// <returns>Parallel writer instance.</returns>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;
            writer.m_Writer = m_MultiHashMapData.AsParallelWriter();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            writer.m_Safety = m_Safety;
#endif
            return writer;
        }

        /// <summary>
        /// Implements parallel writer. Use AsParallelWriter to obtain it from container.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public unsafe struct ParallelWriter
        {
            internal UnsafeMultiHashMap<TKey, TValue>.ParallelWriter m_Writer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
#endif
            /// <summary>
            ///
            /// </summary>
            public int m_ThreadIndex => m_Writer.m_ThreadIndex;

            /// <summary>
            /// The number of items that can fit in the container.
            /// </summary>
            /// <value>The number of items that the container can hold before it resizes its internal storage.</value>
            /// <remarks>Capacity specifies the number of items the container can currently hold. You can change Capacity
            /// to fit more or fewer items. Changing Capacity creates a new array of the specified size, copies the
            /// old array to the new one, and then deallocates the original array memory.</remarks>
            public int Capacity
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                    return m_Writer.Capacity;
                }
            }

            /// <summary>
            /// Add an element with the specified key and value into the container. If the key already exist an ArgumentException will be thrown.
            /// </summary>
            /// <param name="key">The key of the element to add.</param>
            /// <param name="item">The value of the element to add.</param>
            public void Add(TKey key, TValue item)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
                m_Writer.Add(key, item);
            }
        }

        /// <summary>
        /// Returns an enumerator for key that iterates through a container.
        /// </summary>
        /// <param name="key">Key to enumerate values for.</param>
        /// <returns>An IEnumerator object that can be used to iterate through the container.</returns>
        public Enumerator GetValuesForKey(TKey key)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return new Enumerator { hashmap = this, key = key, isFirst = true };
        }

        /// <summary>
        ///
        /// </summary>
        public struct Enumerator : IEnumerator<TValue>
        {
            internal NativeMultiHashMap<TKey, TValue> hashmap;
            internal TKey key;
            internal bool isFirst;

            TValue value;
            NativeMultiHashMapIterator<TKey> iterator;

            /// <summary>
            ///
            /// </summary>
            public void Dispose() { }

            /// <summary>
            ///
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                //Avoids going beyond the end of the collection.
                if (isFirst)
                {
                    isFirst = false;
                    return hashmap.TryGetFirstValue(key, out value, out iterator);
                }

                return hashmap.TryGetNextValue(out value, ref iterator);
            }

            /// <summary>
            ///
            /// </summary>
            public void Reset() => isFirst = true;

            /// <summary>
            ///
            /// </summary>
            public TValue Current => value;

            object IEnumerator.Current => throw new InvalidOperationException("Use IEnumerator<T> to avoid boxing");

            /// <summary>
            ///
            /// </summary>
            /// <returns></returns>
            public Enumerator GetEnumerator() { return this; }
        }

        /// <summary>
        /// Returns an IEnumerator interface for the container.
        /// </summary>
        /// <returns>An IEnumerator interface for the container.</returns>
        public KeyValueEnumerator GetEnumerator()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Safety);
            var ash = m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref ash);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(ash, true);
#endif
            return new KeyValueEnumerator
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = ash,
#endif
                m_Enumerator = new UnsafeHashMapDataEnumerator(m_MultiHashMapData.m_Buffer),
            };
        }

        /// <summary>
        /// This method is not implemented. It will throw NotImplementedException if it is used.
        /// </summary>
        /// <remarks>Use Enumerator GetEnumerator() instead.</remarks>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<KeyValue<TKey, TValue>> IEnumerable<KeyValue<TKey, TValue>>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method is not implemented. It will throw NotImplementedException if it is used.
        /// </summary>
        /// <remarks>Use Enumerator GetEnumerator() instead.</remarks>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implements iterator over the container.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public struct KeyValueEnumerator : IEnumerator<KeyValue<TKey, TValue>>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
#endif
            internal UnsafeHashMapDataEnumerator m_Enumerator;

            /// <summary>
            /// Disposes enumerator.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next element of the container.
            /// </summary>
            /// <returns>Returns true if the iterator is successfully moved to the next element, otherwise it returns false.</returns>
            public unsafe bool MoveNext()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Enumerator.MoveNext();
            }

            /// <summary>
            /// Resets the enumerator to the first element of the container.
            /// </summary>
            public void Reset()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                m_Enumerator.Reset();
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator in the container.
            /// </summary>
            public KeyValue<TKey, TValue> Current
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                    return m_Enumerator.GetCurrent<TKey, TValue>();
                }
            }

            object IEnumerator.Current => throw new InvalidOperationException("Use IEnumerator<KeyValue<TKey, TValue>> to avoid boxing");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
        }
    }

    internal sealed class NativeMultiHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : struct, IEquatable<TKey>, IComparable<TKey>
        where TValue : struct
    {
#if !NET_DOTS
        NativeMultiHashMap<TKey, TValue> m_Target;

        public NativeMultiHashMapDebuggerTypeProxy(NativeMultiHashMap<TKey, TValue> target)
        {
            m_Target = target;
        }

        public List<ListPair<TKey, List<TValue>>> Items
        {
            get
            {
                var result = new List<ListPair<TKey, List<TValue>>>();
                var keys = m_Target.GetUniqueKeyArray(Allocator.Temp);

                using (keys.Item1)
                {
                    for (var k = 0; k < keys.Item2; ++k)
                    {
                        var values = new List<TValue>();
                        if (m_Target.TryGetFirstValue(keys.Item1[k], out var value, out var iterator))
                        {
                            do
                            {
                                values.Add(value);
                            }
                            while (m_Target.TryGetNextValue(out value, ref iterator));
                        }

                        result.Add(new ListPair<TKey, List<TValue>>(keys.Item1[k], values));
                    }
                }

                return result;
            }
        }
#endif
    }
}
