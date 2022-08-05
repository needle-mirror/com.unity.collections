using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Jobs;
using System.Runtime.InteropServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
    internal unsafe struct HashMapHelper<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        [NativeDisableUnsafePtrRestriction]
        internal uint* Ptr;

        [NativeDisableUnsafePtrRestriction]
        internal int* NumItems;

        [NativeDisableUnsafePtrRestriction]
        internal TKey* Keys;

        internal int Count;
        internal int Capacity;
        internal int MinGrowth;
        internal int SizeOfValueT;
        internal AllocatorManager.AllocatorHandle Allocator;

        internal int TotalHashesSizeInBytes => sizeof(uint)*Capacity;
        internal int TotalItemCountSizeInBytes => TotalHashesSizeInBytes;
        internal int TotalHashKeySizeInBytes => TotalHashesSizeInBytes + TotalItemCountSizeInBytes + sizeof(TKey)*Capacity;
        internal int TotalValuesSizeInBytes => SizeOfValueT*Capacity;
        internal int TotalSizeInBytes => TotalHashKeySizeInBytes + TotalValuesSizeInBytes;

        internal bool IsCreated => Ptr != null;

        internal bool IsEmpty => !IsCreated || Count == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetFirstIdx(uint hash)
        {
            return (int)(hash % (uint)Capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int CalcMinCapacity()
        {
            return Count + MinGrowth / 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int CalcCapacity(int capacity)
        {
            var result = Bitwise.AlignUp(Math.Max(Math.Max(Count, 1), capacity), MinGrowth);
            return result;
        }

        internal void Init(int capacity, int sizeOfValueT, int minGrowth, AllocatorManager.AllocatorHandle allocator)
        {
            CollectionHelper.CheckAllocator(allocator);
            Count = 0;
            MinGrowth = math.ceilpow2(minGrowth);
            SizeOfValueT = sizeOfValueT;
            Allocator = allocator;

            Capacity = CalcCapacity(capacity);
            Ptr = (uint*)Memory.Unmanaged.Allocate(TotalSizeInBytes, 16, Allocator);
            NumItems = (int*)(Ptr + Capacity);
            Keys = (TKey*)(Ptr + Capacity * 2);
            Clear();
        }

        internal void Dispose()
        {
            Memory.Unmanaged.Free(Ptr, Allocator);
            Ptr = null;
            NumItems = null;
            Keys = null;
        }

        internal static HashMapHelper<TKey>* Alloc(int capacity, int sizeOfValueT, int minGrowth, AllocatorManager.AllocatorHandle allocator)
        {
            HashMapHelper<TKey>* data = (HashMapHelper<TKey>*)Memory.Unmanaged.Allocate(sizeof(HashMapHelper<TKey>), UnsafeUtility.AlignOf<HashMapHelper<TKey>>(), allocator);
            data->Init(capacity, sizeOfValueT, 256, allocator);

            return data;
        }

        internal static void Free(HashMapHelper<TKey>* data)
        {
            Memory.Unmanaged.Free(data, data->Allocator);
        }

        internal void Clear()
        {
            Count = 0;
            UnsafeUtility.MemSet(Ptr, 0xff, TotalHashesSizeInBytes);
            UnsafeUtility.MemSet(NumItems, 0, TotalItemCountSizeInBytes);
        }

        internal void Resize(int newCapacity)
        {
            var oldCapacity = Capacity;
            var oldPtr = Ptr;
            var oldKeys = Keys;
            var oldCount = Count;

            Capacity = Math.Max(newCapacity, Count);
            Count = 0;

            Ptr = (uint*)Memory.Unmanaged.Allocate(TotalSizeInBytes, 16, Allocator);
            NumItems = (int*)(Ptr + Capacity);
            Keys = (TKey*)(Ptr + Capacity * 2);
            UnsafeUtility.MemSet(Ptr, 0xff, TotalHashesSizeInBytes);
            UnsafeUtility.MemSet(NumItems, 0, TotalItemCountSizeInBytes);

            for (int i = 0, end = oldCapacity; i < end && oldCount > 0; ++i)
            {
                if (oldPtr[i] != uint.MaxValue)
                {
                    var idx = TryAdd(oldKeys[i]);
                    UnsafeUtility.MemCpy(GetElementAt(Ptr, Capacity, idx), GetElementAt(oldPtr, oldCapacity, i), SizeOfValueT);
                    oldCount--;
                }
            }

            Memory.Unmanaged.Free(oldPtr, Allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref TKey GetKeyAt(int idx)
        {
            return ref Keys[idx];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void* GetElementAt(void* src, int capacity, int idx)
        {
            byte* ptr = (byte*)src;
            ptr += capacity * (sizeof(uint) + sizeof(int) + sizeof(TKey));
            ptr += idx * SizeOfValueT;

            return ptr;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        internal ref TValue GetElementAt<TValue>(int idx)
            where TValue : unmanaged
        {
            return ref *(TValue*)GetElementAt(Ptr, Capacity, idx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateCapacity()
        {
            if (CalcMinCapacity() >= Capacity)
            {
                Resize(CalcCapacity(Capacity + MinGrowth));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal uint Hash(in TKey key)
        {
            uint hash = (uint)key.GetHashCode();
            return hash == uint.MaxValue ? 0 : hash;
        }

        internal int TryAdd(in TKey key)
        {
            uint hash = Hash(key);
            int firstIdx = GetFirstIdx(hash);
            int idx = firstIdx;

            do
            {
                uint current = Ptr[idx];

                if (current == uint.MaxValue)
                {
                    Ptr[idx] = hash;
                    GetKeyAt(idx) = key;
                    Count++;

                    NumItems[firstIdx] += 1;

                    return idx;
                }

                if (current == hash && GetKeyAt(idx).Equals(key))
                {
                    return -1;
                }

                idx = (idx + 1) % Capacity;

            } while (idx != firstIdx);

            return -1;
        }

        internal int Find(in TKey key)
        {
            uint hash = Hash(key);
            int firstIdx = GetFirstIdx(hash);
            int num = NumItems[firstIdx];
            int idx = firstIdx;

            do
            {
                if (num == 0)
                {
                    return -1;
                }

                if (Ptr[idx] == hash)
                {
                    if (GetKeyAt(idx).Equals(key))
                    {
                        return idx;
                    }

                    num--;
                }

                idx = (idx + 1) % Capacity;

            } while (idx != firstIdx);

            return -1;
        }

        internal int FindNext(int idx)
        {
            for (int i = idx, end = Capacity; i < end; ++i)
            {
                if (Ptr[i] != uint.MaxValue)
                {
                    return i;
                }
            }

            return -1;
        }

        internal int FindOrAdd(TKey key)
        {
            var idx = Find(key);
            if (-1 != idx)
            {
                return idx;
            }

            return TryAdd(key);
        }

        internal int TryRemove(TKey key)
        {
            var idx = Find(key);
            if (idx != -1)
            {
                Ptr[idx] = uint.MaxValue;
                Count--;

                uint hash = Hash(key);
                int firstIdx = GetFirstIdx(hash);
                NumItems[firstIdx] = math.max(NumItems[firstIdx]-1, 0);
            }

            return idx;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        internal bool TryGetValue<TValue>(TKey key, out TValue item)
            where TValue : unmanaged
        {
            var idx = Find(key);

            if (-1 != idx)
            {
                item = GetElementAt<TValue>(idx);
                return true;
            }

            item = default;
            return false;
        }

        internal NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            var result = new NativeArray<TKey>(Count, allocator.ToAllocator, NativeArrayOptions.UninitializedMemory);

            for (int i = 0, end = Capacity, j = 0, count = Count; i < end && j < count; ++i)
            {
                if (Ptr[i] != uint.MaxValue)
                {
                    result[j] = GetKeyAt(i);
                    j++;
                }
            }

            return result;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        internal NativeArray<TValue> GetValueArray<TValue>(AllocatorManager.AllocatorHandle allocator)
            where TValue : unmanaged
        {
            var result = new NativeArray<TValue>(Count, allocator.ToAllocator, NativeArrayOptions.UninitializedMemory);

            for (int i = 0, end = Capacity, j = 0, count = Count; i < end && j < count; ++i)
            {
                if (Ptr[i] != uint.MaxValue)
                {
                    result[j] = GetElementAt<TValue>(i);
                    j++;
                }
            }

            return result;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        internal NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays<TValue>(AllocatorManager.AllocatorHandle allocator)
            where TValue : unmanaged
        {
            var result = new NativeKeyValueArrays<TKey, TValue>(Count, allocator, NativeArrayOptions.UninitializedMemory);

            for (int i = 0, end = Capacity, j = 0, count = Count; i < end && j < count; ++i)
            {
                if (Ptr[i] != uint.MaxValue)
                {
                    result.Keys[j] = GetKeyAt(i);
                    result.Values[j] = GetElementAt<TValue>(i);
                    j++;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// An unordered, expandable associative array.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerTypeProxy(typeof(UnsafeHashMapDebuggerTypeProxy<,>))]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
    public unsafe struct UnsafeHashMap<TKey, TValue>
        : INativeDisposable
        , IEnumerable<KVPair<TKey, TValue>> // Used by collection initializers.
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal HashMapHelper<TKey> m_Data;

        /// <summary>
        /// Initializes and returns an instance of UnsafeHashMap.
        /// </summary>
        /// <param name="capacity">The number of key-value pairs that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
        public UnsafeHashMap(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_Data = default;
            m_Data.Init(capacity, sizeof(TValue), 256, allocator);
        }

        /// <summary>
        /// Releases all resources (memory).
        /// </summary>
        public void Dispose()
        {
            m_Data.Dispose();
        }


        /// <summary>
        /// Creates and schedules a job that will dispose this hash map.
        /// </summary>
        /// <param name="inputDeps">A job handle. The newly scheduled job will depend upon this handle.</param>
        /// <returns>The handle of a new job that will dispose this hash map.</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new UnsafeDisposeJob { Ptr = m_Data.Ptr, Allocator = m_Data.Allocator }.Schedule(inputDeps);
            m_Data.Ptr = null;

            return jobHandle;
        }

        /// <summary>
        /// Whether this hash map has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this hash map has been allocated (and not yet deallocated).</value>
        public bool IsCreated => m_Data.IsCreated;

        /// <summary>
        /// Whether this hash map is empty.
        /// </summary>
        /// <value>True if this hash map is empty or if the map has not been constructed.</value>
        public bool IsEmpty => m_Data.IsEmpty;

        /// <summary>
        /// The current number of key-value pairs in this hash map.
        /// </summary>
        /// <returns>The current number of key-value pairs in this hash map.</returns>
        public int Count => m_Data.Count;

        /// <summary>
        /// The number of key-value pairs that fit in the current allocation.
        /// </summary>
        /// <value>The number of key-value pairs that fit in the current allocation.</value>
        /// <param name="value">A new capacity. Must be larger than the current capacity.</param>
        /// <exception cref="Exception">Thrown if `value` is less than the current capacity.</exception>
        public int Capacity
        {
            get
            {
                return m_Data.Capacity;
            }

            set
            {
                m_Data.Resize(m_Data.CalcCapacity(value));
            }
        }

        /// <summary>
        /// Removes all key-value pairs.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            m_Data.Clear();
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <remarks>If the key is already present, this method returns false without modifying the hash map.</remarks>
        /// <param name="key">The key to add.</param>
        /// <param name="item">The value to add.</param>
        /// <returns>True if the key-value pair was added.</returns>
        public bool TryAdd(TKey key, TValue item)
        {
            m_Data.UpdateCapacity();

            var idx = m_Data.TryAdd(key);
            if (-1 != idx)
            {
                m_Data.GetElementAt<TValue>(idx) = item;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <remarks>If the key is already present, this method throws without modifying the hash map.</remarks>
        /// <param name="key">The key to add.</param>
        /// <param name="item">The value to add.</param>
        /// <exception cref="ArgumentException">Thrown if the key was already present.</exception>
        public void Add(TKey key, TValue item)
        {
            var result = TryAdd(key, item);

            if (!result)
            {
                ThrowKeyAlreadyAdded(key);
            }
        }

        /// <summary>
        /// Removes a key-value pair.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>True if a key-value pair was removed.</returns>
        public bool Remove(TKey key)
        {
            return -1 != m_Data.TryRemove(key);
        }

        /// <summary>
        /// Returns the value associated with a key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="item">Outputs the value associated with the key. Outputs default if the key was not present.</param>
        /// <returns>True if the key was present.</returns>
        public bool TryGetValue(TKey key, out TValue item)
        {
            return m_Data.TryGetValue(key, out item);
        }

        /// <summary>
        /// Returns true if a given key is present in this hash map.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>True if the key was present.</returns>
        public bool ContainsKey(TKey key)
        {
            return -1 != m_Data.Find(key);
        }

        /// <summary>
        /// Sets the capacity to match what it would be if it had been originally initialized with all its entries.
        /// </summary>
        public void TrimExcess()
        {
            m_Data.Resize(m_Data.CalcCapacity(m_Data.CalcMinCapacity()));
        }

        /// <summary>
        /// Gets and sets values by key.
        /// </summary>
        /// <remarks>Getting a key that is not present will throw. Setting a key that is not already present will add the key.</remarks>
        /// <param name="key">The key to look up.</param>
        /// <value>The value associated with the key.</value>
        /// <exception cref="ArgumentException">For getting, thrown if the key was not present.</exception>
        public TValue this[TKey key]
        {
            get
            {
                TValue result;
                if (!m_Data.TryGetValue(key, out result))
                {
                    ThrowKeyNotPresent(key);
                }

                return result;
            }

            set
            {
                var idx = m_Data.Find(key);
                if (-1 != idx)
                {
                    m_Data.GetElementAt<TValue>(idx) = value;
                    return;
                }

                TryAdd(key, value);
            }
        }

        /// <summary>
        /// Returns an array with a copy of all this hash map's keys (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all this hash map's keys (in no particular order).</returns>
        public NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            return m_Data.GetKeyArray(allocator);
        }

        /// <summary>
        /// Returns an array with a copy of all this hash map's values (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all this hash map's values (in no particular order).</returns>
        public NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            return m_Data.GetValueArray<TValue>(allocator);
        }

        /// <summary>
        /// Returns a NativeKeyValueArrays with a copy of all this hash map's keys and values.
        /// </summary>
        /// <remarks>The key-value pairs are copied in no particular order. For all `i`, `Values[i]` will be the value associated with `Keys[i]`.</remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>A NativeKeyValueArrays with a copy of all this hash map's keys and values.</returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            return m_Data.GetKeyValueArrays<TValue>(allocator);
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator { Data = m_Data, Index = -1 };
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<KVPair<TKey, TValue>> IEnumerable<KVPair<TKey, TValue>>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// An enumerator over the key-value pairs of a container.
        /// </summary>
        /// <remarks>
        /// In an enumerator's initial state, <see cref="Current"/> is not valid to read.
        /// From this state, the first <see cref="MoveNext"/> call advances the enumerator to the first key-value pair.
        /// </remarks>
        public struct Enumerator : IEnumerator<KVPair<TKey, TValue>>
        {
            internal HashMapHelper<TKey> Data;
            internal int Index;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next key-value pair.
            /// </summary>
            /// <returns>True if <see cref="Current"/> is valid to read after the call.</returns>
            public bool MoveNext()
            {
                Index = Data.FindNext(Index + 1);
                return Index != -1;
            }

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset()
            {
                Index = -1;
            }

            /// <summary>
            /// The current key-value pair.
            /// </summary>
            /// <value>The current key-value pair.</value>
            public KVPair<TKey, TValue> Current
            {
                get
                {
                    return new KVPair<TKey, TValue>
                    {
                        Key = Data.GetKeyAt(Index),
                        Value = Data.GetElementAt<TValue>(Index)
                    };
                }
            }

            object IEnumerator.Current => Current;
        }

        /// <summary>
        /// Returns a readonly version of this UnsafeHashMap instance.
        /// </summary>
        /// <remarks>ReadOnly containers point to the same underlying data as the UnsafeHashMap it is made from.</remarks>
        /// <returns>ReadOnly instance for this.</returns>
        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(ref m_Data);
        }

        /// <summary>
        /// A read-only alias for the value of a UnsafeHashMap. Does not have its own allocated storage.
        /// </summary>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public struct ReadOnly
            : IEnumerable<KVPair<TKey, TValue>>
        {
            internal HashMapHelper<TKey> m_Data;

            internal ReadOnly(ref HashMapHelper<TKey> data)
            {
                m_Data = data;
            }

            /// <summary>
            /// Whether this hash map is empty.
            /// </summary>
            /// <value>True if this hash map is empty or if the map has not been constructed.</value>
            public bool IsEmpty => m_Data.IsEmpty;

            /// <summary>
            /// The current number of key-value pairs in this hash map.
            /// </summary>
            /// <returns>The current number of key-value pairs in this hash map.</returns>
            public int Count => m_Data.Count;

            /// <summary>
            /// The number of key-value pairs that fit in the current allocation.
            /// </summary>
            /// <value>The number of key-value pairs that fit in the current allocation.</value>
            public int Capacity => m_Data.Capacity;

            /// <summary>
            /// Returns the value associated with a key.
            /// </summary>
            /// <param name="key">The key to look up.</param>
            /// <param name="item">Outputs the value associated with the key. Outputs default if the key was not present.</param>
            /// <returns>True if the key was present.</returns>
            public bool TryGetValue(TKey key, out TValue item) => m_Data.TryGetValue(key, out item);

            /// <summary>
            /// Returns true if a given key is present in this hash map.
            /// </summary>
            /// <param name="key">The key to look up.</param>
            /// <returns>True if the key was present.</returns>
            public bool ContainsKey(TKey key)
            {
                return -1 != m_Data.Find(key);
            }

            /// <summary>
            /// Gets values by key.
            /// </summary>
            /// <remarks>Getting a key that is not present will throw.</remarks>
            /// <param name="key">The key to look up.</param>
            /// <value>The value associated with the key.</value>
            /// <exception cref="ArgumentException">For getting, thrown if the key was not present.</exception>
            public TValue this[TKey key]
            {
                get
                {
                    TValue result;
                    m_Data.TryGetValue(key, out result);
                    return result;
                }
            }

            /// <summary>
            /// Returns an array with a copy of all this hash map's keys (in no particular order).
            /// </summary>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>An array with a copy of all this hash map's keys (in no particular order).</returns>
            public NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
            {
                return m_Data.GetKeyArray(allocator);
            }

            /// <summary>
            /// Returns an array with a copy of all this hash map's values (in no particular order).
            /// </summary>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>An array with a copy of all this hash map's values (in no particular order).</returns>
            public NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
            {
                return m_Data.GetValueArray<TValue>(allocator);
            }

            /// <summary>
            /// Returns a NativeKeyValueArrays with a copy of all this hash map's keys and values.
            /// </summary>
            /// <remarks>The key-value pairs are copied in no particular order. For all `i`, `Values[i]` will be the value associated with `Keys[i]`.</remarks>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>A NativeKeyValueArrays with a copy of all this hash map's keys and values.</returns>
            public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
            {
                return m_Data.GetKeyValueArrays<TValue>(allocator);
            }

            /// <summary>
            /// Whether this hash map has been allocated (and not yet deallocated).
            /// </summary>
            /// <value>True if this hash map has been allocated (and not yet deallocated).</value>
            public bool IsCreated => m_Data.IsCreated;

            /// <summary>
            /// Returns an enumerator over the key-value pairs of this hash map.
            /// </summary>
            /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
            public Enumerator GetEnumerator()
            {
                return new Enumerator { Data = m_Data, Index = -1 };
            }

            /// <summary>
            /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
            /// </summary>
            /// <returns>Throws NotImplementedException.</returns>
            /// <exception cref="NotImplementedException">Method is not implemented.</exception>
            IEnumerator<KVPair<TKey, TValue>> IEnumerable<KVPair<TKey, TValue>>.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
            /// </summary>
            /// <returns>Throws NotImplementedException.</returns>
            /// <exception cref="NotImplementedException">Method is not implemented.</exception>
            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ThrowKeyNotPresent(TKey key)
        {
            throw new ArgumentException($"Key: {key} is not present.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ThrowKeyAlreadyAdded(TKey key)
        {
            throw new ArgumentException($"An item with the same key has already been added: {key}");
        }
    }

    internal sealed class UnsafeHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
#if !NET_DOTS
        HashMapHelper<TKey> m_Target;

        public UnsafeHashMapDebuggerTypeProxy(UnsafeHashMap<TKey, TValue> target)
        {
            m_Target = target.m_Data;
        }

        public UnsafeHashMapDebuggerTypeProxy(UnsafeHashMap<TKey, TValue>.ReadOnly target)
        {
            m_Target = target.m_Data;
        }

        public List<Pair<TKey, TValue>> Items
        {
            get
            {
                var result = new List<Pair<TKey, TValue>>();
                using (var kva = m_Target.GetKeyValueArrays<TValue>(Allocator.Temp))
                {
                    for (var i = 0; i < kva.Length; ++i)
                    {
                        result.Add(new Pair<TKey, TValue>(kva.Keys[i], kva.Values[i]));
                    }
                }
                return result;
            }
        }
#endif
    }
}
