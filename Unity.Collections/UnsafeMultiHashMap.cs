using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Jobs;
using UnityEngine.Assertions;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Unordered associative array, a collection of keys and values, without any thread safety check features. This container can store multiple values for every key.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the container.</typeparam>
    /// <typeparam name="TValue">The type of the values in the container.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerTypeProxy(typeof(UnsafeMultiHashMapDebuggerTypeProxy<,>))]
    [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
    public unsafe struct UnsafeMultiHashMap<TKey, TValue>
        : INativeDisposable
        , IEnumerable<KeyValue<TKey, TValue>> // Used by collection initializers.
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeHashMapData* m_Buffer;
        internal Allocator m_AllocatorLabel;

        /// <summary>
        /// Constructs a new container with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="capacity">The initial capacity of the container. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        public UnsafeMultiHashMap(int capacity, Allocator allocator)
        {
            m_AllocatorLabel = allocator;
            // Bucket size if bigger to reduce collisions
            UnsafeHashMapData.AllocateHashMap<TKey, TValue>(capacity, capacity * 2, allocator, out m_Buffer);
            Clear();
        }

        /// <summary>
        /// Reports whether container is empty.
        /// </summary>
        /// <value>True if this container empty.</value>
        public bool IsEmpty => !IsCreated || UnsafeHashMapData.IsEmpty(m_Buffer);

        /// <summary>
        /// The current number of items in the container.
        /// </summary>
        /// <returns>The item count.</returns>
        public int Count()
        {
            if (m_Buffer->allocatedIndexLength <= 0)
            {
                return 0;
            }

            return UnsafeHashMapData.GetCount(m_Buffer);
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
                UnsafeHashMapData* data = m_Buffer;
                return data->keyCapacity;
            }

            set
            {
                UnsafeHashMapData* data = m_Buffer;
                UnsafeHashMapData.ReallocateHashMap<TKey, TValue>(data, value, UnsafeHashMapData.GetBucketSize(value), m_AllocatorLabel);
            }
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        /// <remarks>Containers capacity remains unchanged.</remarks>
        public void Clear()
        {
            UnsafeHashMapBase<TKey, TValue>.Clear(m_Buffer);
        }

        /// <summary>
        /// Add an element with the specified key and value into the container.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="item">The value of the element to add.</param>
        public void Add(TKey key, TValue item)
        {
            UnsafeHashMapBase<TKey, TValue>.TryAdd(m_Buffer, key, item, true, m_AllocatorLabel);
        }

        /// <summary>
        /// Removes all elements with the specified key from the container.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>Returns number of removed items.</returns>
        public int Remove(TKey key)
        {
            return UnsafeHashMapBase<TKey, TValue>.Remove(m_Buffer, key, true);
        }

        /// <summary>
        /// Removes all elements with the specified key and value from the container.
        /// </summary>
        /// <typeparam name="TValueEQ"></typeparam>
        /// <param name="key">The key of the element to remove.</param>
        /// <param name="value">The value of the element to remove.</param>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public void Remove<TValueEQ>(TKey key, TValueEQ value)
            where TValueEQ : struct, IEquatable<TValueEQ>
        {
            UnsafeHashMapBase<TKey, TValueEQ>.RemoveKeyValue(m_Buffer, key, value);
        }

        /// <summary>
        /// Removes all elements with the specified iterator the container.
        /// </summary>
        /// <param name="it">Iterator pointing at value to remove.</param>
        public void Remove(NativeMultiHashMapIterator<TKey> it)
        {
            UnsafeHashMapBase<TKey, TValue>.Remove(m_Buffer, it);
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
            return UnsafeHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(m_Buffer, key, out item, out it);
        }

        /// <summary>
        /// Retrieve iterator to the next value for the key.
        /// </summary>
        /// <param name="item">Output value.</param>
        /// <param name="it">Iterator.</param>
        /// <returns>Returns true if next value for the key is found.</returns>
        public bool TryGetNextValue(out TValue item, ref NativeMultiHashMapIterator<TKey> it)
        {
            return UnsafeHashMapBase<TKey, TValue>.TryGetNextValueAtomic(m_Buffer, out item, ref it);
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
            return UnsafeHashMapBase<TKey, TValue>.SetValue(m_Buffer, ref it, ref item);
        }

        /// <summary>
        /// Reports whether memory for the container is allocated.
        /// </summary>
        /// <value>True if this container object's internal storage has been allocated.</value>
        /// <remarks>
        /// Note that the container storage is not created if you use the default constructor. You must specify
        /// at least an allocation type to construct a usable container.
        ///
        /// *Warning:* the `IsCreated` property can't be used to determine whether a copy of a container is still valid.
        /// If you dispose any copy of the container, the container storage is deallocated. However, the properties of
        /// the other copies of the container (including the original) are not updated. As a result the `IsCreated` property
        /// of the copies still return `true` even though the container storage has been deallocated.
        /// </remarks>
        public bool IsCreated => m_Buffer != null;

        /// <summary>
        /// Disposes of this multi-hashmap and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
            UnsafeHashMapData.DeallocateHashMap(m_Buffer, m_AllocatorLabel);
            m_Buffer = null;
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
        [BurstCompatible(RequiredUnityDefine = "UNITY_2020_2_OR_NEWER") /* Due to job scheduling on 2020.1 using statics */]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new UnsafeHashMapDisposeJob { Data = m_Buffer, Allocator = m_AllocatorLabel }.Schedule(inputDeps);
            m_Buffer = null;
            return jobHandle;
        }

        /// <summary>
        /// Returns array populated with keys.
        /// </summary>
        /// <remarks>Number of returned keys will match number of values in the container. If key contains multiple values it will appear number of times
        /// how many values are associated to the same key. If only unique key values desired use GetUniqueKeyArray instead.</remarks>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns>Array of keys.</returns>
        public NativeArray<TKey> GetKeyArray(Allocator allocator)
        {
            var result = new NativeArray<TKey>(Count(), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeHashMapData.GetKeyArray(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// Returns array populated with values.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns>Array of values.</returns>
        public NativeArray<TValue> GetValueArray(Allocator allocator)
        {
            var result = new NativeArray<TValue>(Count(), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeHashMapData.GetValueArray(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// Returns arrays populated with keys and values.
        /// </summary>
        /// <remarks>If key contains multiple values, returned key array will contain multiple identical keys.</remarks>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns>Array of keys-values.</returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(Allocator allocator)
        {
            var result = new NativeKeyValueArrays<TKey, TValue>(Count(), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeHashMapData.GetKeyValueArrays(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// Returns an enumerator for key that iterates through a container.
        /// </summary>
        /// <param name="key">Key to enumerate values for.</param>
        /// <returns>An IEnumerator object that can be used to iterate through the container.</returns>
        public Enumerator GetValuesForKey(TKey key)
        {
            return new Enumerator { hashmap = this, key = key, isFirst = true };
        }

        /// <summary>
        /// Implements iterator over the container.
        /// </summary>
        public struct Enumerator : IEnumerator<TValue>
        {
            internal UnsafeMultiHashMap<TKey, TValue> hashmap;
            internal TKey key;
            internal bool isFirst;

            TValue value;
            NativeMultiHashMapIterator<TKey> iterator;

            /// <summary>
            /// Disposes enumerator.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next element of the container.
            /// </summary>
            /// <returns>Returns true if the iterator is successfully moved to the next element, otherwise it returns false.</returns>
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
            /// Resets the enumerator to the first element of the container.
            /// </summary>
            public void Reset() => isFirst = true;

            /// <summary>
            /// Gets the element at the current position of the enumerator in the container.
            /// </summary>
            public TValue Current => value;

            object IEnumerator.Current => Current;

            /// <summary>
            /// Returns IEnumerator.
            /// </summary>
            /// <returns>An IEnumerator object that can be used to iterate through the container.</returns>
            public Enumerator GetEnumerator() { return this; }
        }

        /// <summary>
        /// Returns parallel writer instance.
        /// </summary>
        /// <returns>Parallel writer instance.</returns>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;

#if UNITY_DOTSRUNTIME
            writer.m_ThreadIndex = -1;    // aggressively check that code-gen has patched the ThreadIndex
#else
            writer.m_ThreadIndex = 0;
#endif
            writer.m_Buffer = m_Buffer;

            return writer;
        }

        /// <summary>
        /// Implements parallel writer. Use AsParallelWriter to obtain it from container.
        /// </summary>
        [NativeContainerIsAtomicWriteOnly]
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public unsafe struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeHashMapData* m_Buffer;

            [NativeSetThreadIndex]
            internal int m_ThreadIndex;

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
                    return m_Buffer->keyCapacity;
                }
            }

            /// <summary>
            /// Add an element with the specified key and value into the container.
            /// </summary>
            /// <param name="key">The key of the element to add.</param>
            /// <param name="item">The value of the element to add.</param>
            public void Add(TKey key, TValue item)
            {
                Assert.IsTrue(m_ThreadIndex >= 0);
                UnsafeHashMapBase<TKey, TValue>.AddAtomicMulti(m_Buffer, key, item, m_ThreadIndex);
            }
        }

        /// <summary>
        /// Returns an IEnumerator interface for the container.
        /// </summary>
        /// <returns>An IEnumerator interface for the container.</returns>
        public KeyValueEnumerator GetEnumerator()
        {
            return new KeyValueEnumerator { m_Enumerator = new UnsafeHashMapDataEnumerator(m_Buffer) };
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
        public struct KeyValueEnumerator : IEnumerator<KeyValue<TKey, TValue>>
        {
            internal UnsafeHashMapDataEnumerator m_Enumerator;

            /// <summary>
            /// Disposes enumerator.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next element of the container.
            /// </summary>
            /// <returns>Returns true if the iterator is successfully moved to the next element, otherwise it returns false.</returns>
            public bool MoveNext() => m_Enumerator.MoveNext();

            /// <summary>
            /// Resets the enumerator to the first element of the container.
            /// </summary>
            public void Reset() => m_Enumerator.Reset();

            /// <summary>
            /// Gets the element at the current position of the enumerator in the container.
            /// </summary>
            public KeyValue<TKey, TValue> Current => m_Enumerator.GetCurrent<TKey, TValue>();

            object IEnumerator.Current => Current;
        }
    }

    internal sealed class UnsafeMultiHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : struct, IEquatable<TKey>, IComparable<TKey>
        where TValue : struct
    {
#if !NET_DOTS
        UnsafeMultiHashMap<TKey, TValue> m_Target;

        public UnsafeMultiHashMapDebuggerTypeProxy(UnsafeMultiHashMap<TKey, TValue> target)
        {
            m_Target = target;
        }

        public static (NativeArray<TKey>, int) GetUniqueKeyArray(ref UnsafeMultiHashMap<TKey, TValue> hashMap, Allocator allocator)
        {
            var withDuplicates = hashMap.GetKeyArray(allocator);
            withDuplicates.Sort();
            int uniques = withDuplicates.Unique();
            return (withDuplicates, uniques);
        }

        public List<ListPair<TKey, List<TValue>>> Items
        {
            get
            {
                var result = new List<ListPair<TKey, List<TValue>>>();
                var keys = GetUniqueKeyArray(ref m_Target, Allocator.Temp);

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
