using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    ///
    /// </summary>
    public unsafe struct UnsafeHashMapBucketData
    {
        internal UnsafeHashMapBucketData(byte* v, byte* k, byte* n, byte* b, int bcm)
        {
            values = v;
            keys = k;
            next = n;
            buckets = b;
            bucketCapacityMask = bcm;
        }

        /// <summary>
        ///
        /// </summary>
        public readonly byte* values;

        /// <summary>
        ///
        /// </summary>
        public readonly byte* keys;

        /// <summary>
        ///
        /// </summary>
        public readonly byte* next;

        /// <summary>
        ///
        /// </summary>
        public readonly byte* buckets;

        /// <summary>
        ///
        /// </summary>
        public readonly int bucketCapacityMask;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct UnsafeHashMapData
    {
        [FieldOffset(0)]
        internal byte* values;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(8)]
        internal byte* keys;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(16)]
        internal byte* next;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(24)]
        internal byte* buckets;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(32)]
        internal int keyCapacity;

        [FieldOffset(36)]
        internal int bucketCapacityMask; // = bucket capacity - 1

        [FieldOffset(40)]
        internal int allocatedIndexLength;

        [FieldOffset(JobsUtility.CacheLineSize < 64 ? 64 : JobsUtility.CacheLineSize)]
        internal fixed int firstFreeTLS[JobsUtility.MaxJobThreadCount * IntsPerCacheLine];

        // 64 is the cache line size on x86, arm usually has 32 - so it is possible to save some memory there
        internal const int IntsPerCacheLine = JobsUtility.CacheLineSize / sizeof(int);

        internal static int GetBucketSize(int capacity)
        {
            return capacity * 2;
        }

        internal static int GrowCapacity(int capacity)
        {
            if (capacity == 0)
            {
                return 1;
            }

            return capacity * 2;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void IsBlittableAndThrow<TKey, TValue>()
            where TKey : struct
            where TValue : struct
        {
            CollectionHelper.CheckIsUnmanaged<TKey>();
            CollectionHelper.CheckIsUnmanaged<TValue>();
        }

        internal static void AllocateHashMap<TKey, TValue>(int length, int bucketLength, Allocator label,
            out UnsafeHashMapData* outBuf)
            where TKey : struct
            where TValue : struct
        {
            IsBlittableAndThrow<TKey, TValue>();

            UnsafeHashMapData* data = (UnsafeHashMapData*)UnsafeUtility.Malloc(sizeof(UnsafeHashMapData), UnsafeUtility.AlignOf<UnsafeHashMapData>(), label);

            bucketLength = math.ceilpow2(bucketLength);

            data->keyCapacity = length;
            data->bucketCapacityMask = bucketLength - 1;

            int keyOffset, nextOffset, bucketOffset;
            int totalSize = CalculateDataSize<TKey, TValue>(length, bucketLength, out keyOffset, out nextOffset, out bucketOffset);

            data->values = (byte*)UnsafeUtility.Malloc(totalSize, JobsUtility.CacheLineSize, label);
            data->keys = data->values + keyOffset;
            data->next = data->values + nextOffset;
            data->buckets = data->values + bucketOffset;

            outBuf = data;
        }

        internal static void ReallocateHashMap<TKey, TValue>(UnsafeHashMapData* data, int newCapacity, int newBucketCapacity, Allocator label)
            where TKey : struct
            where TValue : struct
        {
            newBucketCapacity = math.ceilpow2(newBucketCapacity);

            if (data->keyCapacity == newCapacity && (data->bucketCapacityMask + 1) == newBucketCapacity)
            {
                return;
            }

            CheckHashMapReallocateDoesNotShrink(data, newCapacity);

            int keyOffset, nextOffset, bucketOffset;
            int totalSize = CalculateDataSize<TKey, TValue>(newCapacity, newBucketCapacity, out keyOffset, out nextOffset, out bucketOffset);

            byte* newData = (byte*)UnsafeUtility.Malloc(totalSize, JobsUtility.CacheLineSize, label);
            byte* newKeys = newData + keyOffset;
            byte* newNext = newData + nextOffset;
            byte* newBuckets = newData + bucketOffset;

            // The items are taken from a free-list and might not be tightly packed, copy all of the old capcity
            UnsafeUtility.MemCpy(newData, data->values, data->keyCapacity * UnsafeUtility.SizeOf<TValue>());
            UnsafeUtility.MemCpy(newKeys, data->keys, data->keyCapacity * UnsafeUtility.SizeOf<TKey>());
            UnsafeUtility.MemCpy(newNext, data->next, data->keyCapacity * UnsafeUtility.SizeOf<int>());

            for (int emptyNext = data->keyCapacity; emptyNext < newCapacity; ++emptyNext)
            {
                ((int*)newNext)[emptyNext] = -1;
            }

            // re-hash the buckets, first clear the new bucket list, then insert all values from the old list
            for (int bucket = 0; bucket < newBucketCapacity; ++bucket)
            {
                ((int*)newBuckets)[bucket] = -1;
            }

            for (int bucket = 0; bucket <= data->bucketCapacityMask; ++bucket)
            {
                int* buckets = (int*)data->buckets;
                int* nextPtrs = (int*)newNext;
                while (buckets[bucket] >= 0)
                {
                    int curEntry = buckets[bucket];
                    buckets[bucket] = nextPtrs[curEntry];
                    int newBucket = UnsafeUtility.ReadArrayElement<TKey>(data->keys, curEntry).GetHashCode() & (newBucketCapacity - 1);
                    nextPtrs[curEntry] = ((int*)newBuckets)[newBucket];
                    ((int*)newBuckets)[newBucket] = curEntry;
                }
            }

            UnsafeUtility.Free(data->values, label);
            if (data->allocatedIndexLength > data->keyCapacity)
            {
                data->allocatedIndexLength = data->keyCapacity;
            }

            data->values = newData;
            data->keys = newKeys;
            data->next = newNext;
            data->buckets = newBuckets;
            data->keyCapacity = newCapacity;
            data->bucketCapacityMask = newBucketCapacity - 1;
        }

        internal static void DeallocateHashMap(UnsafeHashMapData* data, Allocator allocator)
        {
            UnsafeUtility.Free(data->values, allocator);
            UnsafeUtility.Free(data, allocator);
        }

        internal static int CalculateDataSize<TKey, TValue>(int length, int bucketLength, out int keyOffset, out int nextOffset, out int bucketOffset)
            where TKey : struct
            where TValue : struct
        {
            var sizeOfTValue = UnsafeUtility.SizeOf<TValue>();
            var sizeOfTKey = UnsafeUtility.SizeOf<TKey>();
            var sizeOfInt = UnsafeUtility.SizeOf<int>();

            var valuesSize = CollectionHelper.Align(sizeOfTValue * length, JobsUtility.CacheLineSize);
            var keysSize = CollectionHelper.Align(sizeOfTKey * length, JobsUtility.CacheLineSize);
            var nextSize = CollectionHelper.Align(sizeOfInt * length, JobsUtility.CacheLineSize);
            var bucketSize = CollectionHelper.Align(sizeOfInt * bucketLength, JobsUtility.CacheLineSize);
            var totalSize = valuesSize + keysSize + nextSize + bucketSize;

            keyOffset = 0 + valuesSize;
            nextOffset = keyOffset + keysSize;
            bucketOffset = nextOffset + nextSize;

            return totalSize;
        }

        internal static bool IsEmpty(UnsafeHashMapData* data)
        {
            if (data->allocatedIndexLength <= 0)
            {
                return true;
            }

            var bucketArray = (int*)data->buckets;
            var bucketNext = (int*)data->next;
            var capacityMask = data->bucketCapacityMask;

            for (int i = 0; i <= capacityMask; ++i)
            {
                int bucket = bucketArray[i];

                if (bucket != -1)
                {
                    return false;
                }
            }

            return true;
        }

        internal static int GetCount(UnsafeHashMapData* data)
        {
            var bucketArray = (int*)data->buckets;
            var bucketNext = (int*)data->next;
            var capacityMask = data->bucketCapacityMask;

            int count = 0;
            for (int i = 0; i <= capacityMask; ++i)
            {
                int bucket = bucketArray[i];

                while (bucket != -1)
                {
                    count++;
                    bucket = bucketNext[bucket];
                }
            }

            return count;
        }

        internal static bool MoveNext(UnsafeHashMapData* data, ref int bucketIndex, ref int nextIndex, out int index)
        {
            var bucketArray = (int*)data->buckets;
            var bucketNext = (int*)data->next;
            var capacityMask = data->bucketCapacityMask;

            if (nextIndex != -1)
            {
                index = nextIndex;
                nextIndex = bucketNext[nextIndex];
                return true;
            }

            for (int i = bucketIndex; i <= capacityMask; ++i)
            {
                var idx = bucketArray[i];

                if (idx != -1)
                {
                    index = idx;
                    bucketIndex = i + 1;
                    nextIndex = bucketNext[idx];

                    return true;
                }
            }

            index = -1;
            bucketIndex = capacityMask + 1;
            nextIndex = -1;
            return false;
        }

        internal static void GetKeyArray<TKey>(UnsafeHashMapData* data, NativeArray<TKey> result)
            where TKey : struct
        {
            var bucketArray = (int*)data->buckets;
            var bucketNext = (int*)data->next;

            int count = 0;
            for (int i = 0; i <= data->bucketCapacityMask; ++i)
            {
                int bucket = bucketArray[i];

                while (bucket != -1)
                {
                    result[count++] = UnsafeUtility.ReadArrayElement<TKey>(data->keys, bucket);
                    bucket = bucketNext[bucket];
                }
            }
        }

        internal static void GetValueArray<TValue>(UnsafeHashMapData* data, NativeArray<TValue> result)
            where TValue : struct
        {
            var bucketArray = (int*)data->buckets;
            var bucketNext = (int*)data->next;

            int count = 0;
            for (int i = 0; i <= data->bucketCapacityMask; ++i)
            {
                int bucket = bucketArray[i];

                while (bucket != -1)
                {
                    result[count++] = UnsafeUtility.ReadArrayElement<TValue>(data->values, bucket);
                    bucket = bucketNext[bucket];
                }
            }
        }

        internal static void GetKeyValueArrays<TKey, TValue>(UnsafeHashMapData* data, NativeKeyValueArrays<TKey, TValue> result)
            where TKey : struct
            where TValue : struct
        {
            var bucketArray = (int*)data->buckets;
            var bucketNext = (int*)data->next;

            var count = 0;
            for (int i = 0; i <= data->bucketCapacityMask; ++i)
            {
                int bucket = bucketArray[i];

                while (bucket != -1)
                {
                    result.Keys[count] = UnsafeUtility.ReadArrayElement<TKey>(data->keys, bucket);
                    result.Values[count] = UnsafeUtility.ReadArrayElement<TValue>(data->values, bucket);
                    count++;
                    bucket = bucketNext[bucket];
                }
            }
        }

        internal UnsafeHashMapBucketData GetBucketData()
        {
            return new UnsafeHashMapBucketData(values, keys, next, buckets, bucketCapacityMask);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckHashMapReallocateDoesNotShrink(UnsafeHashMapData* data, int newCapacity)
        {
            if (data->keyCapacity > newCapacity)
                throw new Exception("Shrinking a hash map is not supported");
        }
    }

    [NativeContainer]
    internal unsafe struct UnsafeHashMapDataDispose
    {
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeHashMapData* m_Buffer;
        internal Allocator m_AllocatorLabel;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif

        public void Dispose()
        {
            UnsafeHashMapData.DeallocateHashMap(m_Buffer, m_AllocatorLabel);
        }
    }

    [BurstCompile]
    internal unsafe struct UnsafeHashMapDataDisposeJob : IJob
    {
        internal UnsafeHashMapDataDispose Data;

        public void Execute()
        {
            Data.Dispose();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct UnsafeHashMapBase<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        internal static unsafe void Clear(UnsafeHashMapData* data)
        {
            UnsafeUtility.MemSet(data->buckets, 0xff, (data->bucketCapacityMask + 1) * 4);
            UnsafeUtility.MemSet(data->next, 0xff, (data->keyCapacity) * 4);

            for (int tls = 0; tls < JobsUtility.MaxJobThreadCount; ++tls)
            {
                data->firstFreeTLS[tls * UnsafeHashMapData.IntsPerCacheLine] = -1;
            }

            data->allocatedIndexLength = 0;
        }

        internal static unsafe int AllocEntry(UnsafeHashMapData* data, int threadIndex)
        {
            int idx;
            int* nextPtrs = (int*)data->next;

            do
            {
                idx = data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntsPerCacheLine];

                // Check if this thread has a free entry. Negative value means there is nothing free.
                if (idx < 0)
                {
                    // Try to refill local cache. The local cache is a linked list of 16 free entries.

                    // Indicate to other threads that we are refilling the cache.
                    // -2 means refilling cache.
                    // -1 means nothing free on this thread.
                    Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntsPerCacheLine], -2);

                    // If it failed try to get one from the never-allocated array
                    if (data->allocatedIndexLength < data->keyCapacity)
                    {
                        idx = Interlocked.Add(ref data->allocatedIndexLength, 16) - 16;

                        if (idx < data->keyCapacity - 1)
                        {
                            int count = math.min(16, data->keyCapacity - idx);

                            // Set up a linked list of free entries.
                            for (int i = 1; i < count; ++i)
                            {
                                nextPtrs[idx + i] = idx + i + 1;
                            }

                            // Last entry points to null.
                            nextPtrs[idx + count - 1] = -1;

                            // The first entry is going to be allocated to someone so it also points to null.
                            nextPtrs[idx] = -1;

                            // Set the TLS first free to the head of the list, which is the one after the entry we are returning.
                            Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntsPerCacheLine], idx + 1);

                            return idx;
                        }

                        if (idx == data->keyCapacity - 1)
                        {
                            // We tried to allocate more entries for this thread but we've already hit the key capacity,
                            // so we are in fact out of space. Record that this thread has no more entries.
                            Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntsPerCacheLine], -1);

                            return idx;
                        }
                    }

                    // If we reach here, then we couldn't allocate more entries for this thread, so it's completely empty.
                    Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntsPerCacheLine], -1);

                    // Failed to get any, try to get one from another free list
                    bool again = true;
                    while (again)
                    {
                        again = false;
                        for (int other = (threadIndex + 1) % JobsUtility.MaxJobThreadCount
                             ; other != threadIndex
                             ; other = (other + 1) % JobsUtility.MaxJobThreadCount
                        )
                        {
                            // Attempt to grab a free entry from another thread and switch the other thread's free head
                            // atomically.
                            do
                            {
                                idx = data->firstFreeTLS[other * UnsafeHashMapData.IntsPerCacheLine];

                                if (idx < 0)
                                {
                                    break;
                                }
                            }
                            while (Interlocked.CompareExchange(
                                ref data->firstFreeTLS[other * UnsafeHashMapData.IntsPerCacheLine]
                                , nextPtrs[idx]
                                , idx
                                   ) != idx
                            );

                            if (idx == -2)
                            {
                                // If the thread was refilling the cache, then try again.
                                again = true;
                            }
                            else if (idx >= 0)
                            {
                                // We succeeded in getting an entry from another thread so remove this entry from the
                                // linked list.
                                nextPtrs[idx] = -1;
                                return idx;
                            }
                        }
                    }
                    ThrowFull();
                }

                CheckOutOfCapacity(idx, data->keyCapacity);
            }
            while (Interlocked.CompareExchange(
                ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntsPerCacheLine]
                , nextPtrs[idx]
                , idx
                   ) != idx
            );

            nextPtrs[idx] = -1;
            return idx;
        }

        internal static unsafe void FreeEntry(UnsafeHashMapData* data, int idx, int threadIndex)
        {
            int* nextPtrs = (int*)data->next;
            int next = -1;

            do
            {
                next = data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntsPerCacheLine];
                nextPtrs[idx] = next;
            }
            while (Interlocked.CompareExchange(
                ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntsPerCacheLine]
                , idx
                , next
                   ) != next
            );
        }

        internal static unsafe bool TryAddAtomic(UnsafeHashMapData* data, TKey key, TValue item, int threadIndex)
        {
            TValue tempItem;
            NativeMultiHashMapIterator<TKey> tempIt;
            if (TryGetFirstValueAtomic(data, key, out tempItem, out tempIt))
            {
                return false;
            }

            // Allocate an entry from the free list
            int idx = AllocEntry(data, threadIndex);

            // Write the new value to the entry
            UnsafeUtility.WriteArrayElement(data->keys, idx, key);
            UnsafeUtility.WriteArrayElement(data->values, idx, item);

            int bucket = key.GetHashCode() & data->bucketCapacityMask;
            // Add the index to the hash-map
            int* buckets = (int*)data->buckets;

            // Make the bucket's head idx. If the exchange returns something other than -1, then the bucket had
            // a non-null head which means we need to do more checks...
            if (Interlocked.CompareExchange(ref buckets[bucket], idx, -1) != -1)
            {
                int* nextPtrs = (int*)data->next;
                int next = -1;

                do
                {
                    // Link up this entry with the rest of the bucket under the assumption that this key
                    // doesn't already exist in the bucket. This assumption could be wrong, which will be
                    // checked later.
                    next = buckets[bucket];
                    nextPtrs[idx] = next;

                    // If the key already exists then we should free the entry we took earlier.
                    if (TryGetFirstValueAtomic(data, key, out tempItem, out tempIt))
                    {
                        // Put back the entry in the free list if someone else added it while trying to add
                        FreeEntry(data, idx, threadIndex);

                        return false;
                    }
                }
                while (Interlocked.CompareExchange(ref buckets[bucket], idx, next) != next);
            }

            return true;
        }

        internal static unsafe void AddAtomicMulti(UnsafeHashMapData* data, TKey key, TValue item, int threadIndex)
        {
            // Allocate an entry from the free list
            int idx = AllocEntry(data, threadIndex);

            // Write the new value to the entry
            UnsafeUtility.WriteArrayElement(data->keys, idx, key);
            UnsafeUtility.WriteArrayElement(data->values, idx, item);

            int bucket = key.GetHashCode() & data->bucketCapacityMask;
            // Add the index to the hash-map
            int* buckets = (int*)data->buckets;

            int nextPtr;
            int* nextPtrs = (int*)data->next;
            do
            {
                nextPtr = buckets[bucket];
                nextPtrs[idx] = nextPtr;
            }
            while (Interlocked.CompareExchange(ref buckets[bucket], idx, nextPtr) != nextPtr);
        }

        internal static unsafe bool TryAdd(UnsafeHashMapData* data, TKey key, TValue item, bool isMultiHashMap, Allocator allocation)
        {
            TValue tempItem;
            NativeMultiHashMapIterator<TKey> tempIt;
            if (!isMultiHashMap && TryGetFirstValueAtomic(data, key, out tempItem, out tempIt))
            {
                return false;
            }

            // Allocate an entry from the free list
            int idx;
            int* nextPtrs;

            if (data->allocatedIndexLength >= data->keyCapacity && data->firstFreeTLS[0] < 0)
            {
                for (int tls = 1; tls < JobsUtility.MaxJobThreadCount; ++tls)
                {
                    if (data->firstFreeTLS[tls * UnsafeHashMapData.IntsPerCacheLine] >= 0)
                    {
                        idx = data->firstFreeTLS[tls * UnsafeHashMapData.IntsPerCacheLine];
                        nextPtrs = (int*)data->next;
                        data->firstFreeTLS[tls * UnsafeHashMapData.IntsPerCacheLine] = nextPtrs[idx];
                        nextPtrs[idx] = -1;
                        data->firstFreeTLS[0] = idx;
                        break;
                    }
                }

                if (data->firstFreeTLS[0] < 0)
                {
                    int newCap = UnsafeHashMapData.GrowCapacity(data->keyCapacity);
                    UnsafeHashMapData.ReallocateHashMap<TKey, TValue>(data, newCap, UnsafeHashMapData.GetBucketSize(newCap), allocation);
                }
            }

            idx = data->firstFreeTLS[0];

            if (idx >= 0)
            {
                data->firstFreeTLS[0] = ((int*)data->next)[idx];
            }
            else
            {
                idx = data->allocatedIndexLength++;
            }

            CheckIndexOutOfBounds(data, idx);

            // Write the new value to the entry
            UnsafeUtility.WriteArrayElement(data->keys, idx, key);
            UnsafeUtility.WriteArrayElement(data->values, idx, item);

            int bucket = key.GetHashCode() & data->bucketCapacityMask;
            // Add the index to the hash-map
            int* buckets = (int*)data->buckets;
            nextPtrs = (int*)data->next;

            nextPtrs[idx] = buckets[bucket];
            buckets[bucket] = idx;

            return true;
        }

        internal static unsafe int Remove(UnsafeHashMapData* data, TKey key, bool isMultiHashMap)
        {
            if (data->keyCapacity == 0)
            {
                return 0;
            }

            var removed = 0;

            // First find the slot based on the hash
            var buckets = (int*)data->buckets;
            var nextPtrs = (int*)data->next;
            var bucket = key.GetHashCode() & data->bucketCapacityMask;
            var prevEntry = -1;
            var entryIdx = buckets[bucket];

            while (entryIdx >= 0 && entryIdx < data->keyCapacity)
            {
                if (UnsafeUtility.ReadArrayElement<TKey>(data->keys, entryIdx).Equals(key))
                {
                    ++removed;

                    // Found matching element, remove it
                    if (prevEntry < 0)
                    {
                        buckets[bucket] = nextPtrs[entryIdx];
                    }
                    else
                    {
                        nextPtrs[prevEntry] = nextPtrs[entryIdx];
                    }

                    // And free the index
                    int nextIdx = nextPtrs[entryIdx];
                    nextPtrs[entryIdx] = data->firstFreeTLS[0];
                    data->firstFreeTLS[0] = entryIdx;
                    entryIdx = nextIdx;

                    // Can only be one hit in regular hashmaps, so return
                    if (!isMultiHashMap)
                    {
                        break;
                    }
                }
                else
                {
                    prevEntry = entryIdx;
                    entryIdx = nextPtrs[entryIdx];
                }
            }

            return removed;
        }

        internal static unsafe void Remove(UnsafeHashMapData* data, NativeMultiHashMapIterator<TKey> it)
        {
            // First find the slot based on the hash
            int* buckets = (int*)data->buckets;
            int* nextPtrs = (int*)data->next;
            int bucket = it.key.GetHashCode() & data->bucketCapacityMask;

            int entryIdx = buckets[bucket];

            if (entryIdx == it.EntryIndex)
            {
                buckets[bucket] = nextPtrs[entryIdx];
            }
            else
            {
                while (entryIdx >= 0 && nextPtrs[entryIdx] != it.EntryIndex)
                {
                    entryIdx = nextPtrs[entryIdx];
                }

                if (entryIdx < 0)
                {
                    ThrowInvalidIterator();
                }

                nextPtrs[entryIdx] = nextPtrs[it.EntryIndex];
            }

            // And free the index
            nextPtrs[it.EntryIndex] = data->firstFreeTLS[0];
            data->firstFreeTLS[0] = it.EntryIndex;
        }

        internal static unsafe void RemoveKeyValue<TValueEQ>(UnsafeHashMapData* data, TKey key, TValueEQ value)
            where TValueEQ : struct, IEquatable<TValueEQ>
        {
            if (data->keyCapacity == 0)
            {
                return;
            }

            var buckets = (int*)data->buckets;
            var keyCapacity = (uint)data->keyCapacity;
            var prevNextPtr = buckets + (key.GetHashCode() & data->bucketCapacityMask);
            var entryIdx = *prevNextPtr;

            if ((uint)entryIdx >= keyCapacity)
            {
                return;
            }

            var nextPtrs = (int*)data->next;
            var keys = data->keys;
            var values = data->values;
            var firstFreeTLS = data->firstFreeTLS;

            do
            {
                if (UnsafeUtility.ReadArrayElement<TKey>(keys, entryIdx).Equals(key)
                    && UnsafeUtility.ReadArrayElement<TValueEQ>(values, entryIdx).Equals(value))
                {
                    int nextIdx = nextPtrs[entryIdx];
                    nextPtrs[entryIdx] = firstFreeTLS[0];
                    firstFreeTLS[0] = entryIdx;
                    *prevNextPtr = entryIdx = nextIdx;
                }
                else
                {
                    prevNextPtr = nextPtrs + entryIdx;
                    entryIdx = *prevNextPtr;
                }
            }
            while ((uint)entryIdx < keyCapacity);
        }

        internal static unsafe bool TryGetFirstValueAtomic(UnsafeHashMapData* data, TKey key, out TValue item, out NativeMultiHashMapIterator<TKey> it)
        {
            it.key = key;

            if (data->allocatedIndexLength <= 0)
            {
                it.EntryIndex = it.NextEntryIndex = -1;
                item = default;
                return false;
            }

            // First find the slot based on the hash
            int* buckets = (int*)data->buckets;
            int bucket = key.GetHashCode() & data->bucketCapacityMask;
            it.EntryIndex = it.NextEntryIndex = buckets[bucket];
            return TryGetNextValueAtomic(data, out item, ref it);
        }

        internal static unsafe bool TryGetNextValueAtomic(UnsafeHashMapData* data, out TValue item, ref NativeMultiHashMapIterator<TKey> it)
        {
            int entryIdx = it.NextEntryIndex;
            it.NextEntryIndex = -1;
            it.EntryIndex = -1;
            item = default;
            if (entryIdx < 0 || entryIdx >= data->keyCapacity)
            {
                return false;
            }

            int* nextPtrs = (int*)data->next;
            while (!UnsafeUtility.ReadArrayElement<TKey>(data->keys, entryIdx).Equals(it.key))
            {
                entryIdx = nextPtrs[entryIdx];
                if (entryIdx < 0 || entryIdx >= data->keyCapacity)
                {
                    return false;
                }
            }

            it.NextEntryIndex = nextPtrs[entryIdx];
            it.EntryIndex = entryIdx;

            // Read the value
            item = UnsafeUtility.ReadArrayElement<TValue>(data->values, entryIdx);

            return true;
        }

        internal static unsafe bool SetValue(UnsafeHashMapData* data, ref NativeMultiHashMapIterator<TKey> it, ref TValue item)
        {
            int entryIdx = it.EntryIndex;
            if (entryIdx < 0 || entryIdx >= data->keyCapacity)
            {
                return false;
            }

            UnsafeUtility.WriteArrayElement(data->values, entryIdx, item);
            return true;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckOutOfCapacity(int idx, int keyCapacity)
        {
            if (idx >= keyCapacity)
            {
                throw new InvalidOperationException(string.Format("nextPtr idx {0} beyond capacity {1}", idx, keyCapacity));
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static unsafe void CheckIndexOutOfBounds(UnsafeHashMapData* data, int idx)
        {
            if (idx < 0 || idx >= data->keyCapacity)
                throw new InvalidOperationException("Internal HashMap error");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ThrowFull()
        {
            throw new InvalidOperationException("HashMap is full");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ThrowInvalidIterator()
        {
            throw new InvalidOperationException("Invalid iterator passed to HashMap remove");
        }
    }

    /// <summary>
    /// Defines a key/value pair retrieved by enumerator.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the container.</typeparam>
    /// <typeparam name="TValue">The type of the values in the container.</typeparam>
    [DebuggerDisplay("Key = {Key}, Value = {Value}")]
    public unsafe struct KeyValue<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        internal UnsafeHashMapData* m_Buffer;
        internal int m_Index;
        internal int m_Next;

        /// <summary>
        /// Key of key/value pair.
        /// </summary>
        public TKey Key
        {
            get
            {
                if (m_Index != -1)
                {
                    return UnsafeUtility.ReadArrayElement<TKey>(m_Buffer->keys, m_Index);
                }

                return default;
            }
        }

        /// <summary>
        /// Value of key/value pair.
        /// </summary>
        public TValue Value
        {
            get
            {
                if (m_Index != -1)
                {
                    return UnsafeUtility.ReadArrayElement<TValue>(m_Buffer->values, m_Index);
                }

                return default;
            }
        }

        /// <summary>
        /// Retrives both key and value.
        /// </summary>
        /// <param name="key">Key of key/value pair.</param>
        /// <param name="value">Value of key/value pair.</param>
        /// <returns>Returns true if key/value pair is valid.</returns>
        public bool GetKeyValue(out TKey key, out TValue value)
        {
            if (m_Index != -1)
            {
                key = UnsafeUtility.ReadArrayElement<TKey>(m_Buffer->keys, m_Index);
                value = UnsafeUtility.ReadArrayElement<TValue>(m_Buffer->values, m_Index);
                return true;
            }

            key = default;
            value = default;
            return false;
        }
    }

    internal unsafe struct UnsafeHashMapDataEnumerator
    {
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeHashMapData* m_Buffer;
        internal int m_Index;
        internal int m_BucketIndex;
        internal int m_NextIndex;

        internal unsafe UnsafeHashMapDataEnumerator(UnsafeHashMapData* data)
        {
            m_Buffer = data;
            m_Index = -1;
            m_BucketIndex = 0;
            m_NextIndex = -1;
        }

        internal bool MoveNext()
        {
            return UnsafeHashMapData.MoveNext(m_Buffer, ref m_BucketIndex, ref m_NextIndex, out m_Index);
        }

        internal void Reset()
        {
            m_Index = -1;
            m_BucketIndex = 0;
            m_NextIndex = -1;
        }

        internal KeyValue<TKey, TValue> GetCurrent<TKey, TValue>()
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            return new KeyValue<TKey, TValue> { m_Buffer = m_Buffer, m_Index = m_Index };
        }

        internal TKey GetCurrentKey<TKey>()
            where TKey : struct, IEquatable<TKey>
        {
            if (m_Index != -1)
            {
                return UnsafeUtility.ReadArrayElement<TKey>(m_Buffer->keys, m_Index);
            }

            return default;
        }
    }

    /// <summary>
    /// Unordered associative array, a collection of keys and values, without any thread safety check features.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the container.</typeparam>
    /// <typeparam name="TValue">The type of the values in the container.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Count = {Count()}, Capacity = {Capacity}, IsCreated = {IsCreated}, IsEmpty = {IsEmpty}")]
    [DebuggerTypeProxy(typeof(UnsafeHashMapDebuggerTypeProxy<,>))]
    public unsafe struct UnsafeHashMap<TKey, TValue>
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
        public UnsafeHashMap(int capacity, Allocator allocator)
        {
            CollectionHelper.CheckIsUnmanaged<TKey>();
            CollectionHelper.CheckIsUnmanaged<TValue>();

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
        /// Try adding an element with the specified key and value into the container. If the key already exist, the value won't be updated.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="item">The value of the element to add.</param>
        /// <returns>Returns true if value is added into the container, otherwise returns false.</returns>
        public bool TryAdd(TKey key, TValue item)
        {
            return UnsafeHashMapBase<TKey, TValue>.TryAdd(m_Buffer, key, item, false, m_AllocatorLabel);
        }

        /// <summary>
        /// Add an element with the specified key and value into the container.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="item">The value of the element to add.</param>
        public void Add(TKey key, TValue item)
        {
            TryAdd(key, item);
        }

        /// <summary>
        /// Removes the element with the specified key from the container.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>Returns true if the key was removed from the container, otherwise returns false indicating key wasn't in the container.</returns>
        public bool Remove(TKey key)
        {
            return UnsafeHashMapBase<TKey, TValue>.Remove(m_Buffer, key, false) != 0;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="item">If key is found item parameter will contain value</param>
        /// <returns>Returns true if key is found, otherwise returns false.</returns>
        public bool TryGetValue(TKey key, out TValue item)
        {
            NativeMultiHashMapIterator<TKey> tempIt;
            return UnsafeHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(m_Buffer, key, out item, out tempIt);
        }

        /// <summary>
        /// Determines whether an key is in the container.
        /// </summary>
        /// <param name="key">The key to locate in the container.</param>
        /// <returns>Returns true if the container contains the key.</returns>
        public bool ContainsKey(TKey key)
        {
            return UnsafeHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(m_Buffer, key, out var tempValue, out var tempIt);
        }

        /// <summary>
        /// Retrieve a value from container by key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get
            {
                TValue res;
                TryGetValue(key, out res);
                return res;
            }

            set
            {
                if (UnsafeHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(m_Buffer, key, out var item, out var iterator))
                {
                    UnsafeHashMapBase<TKey, TValue>.SetValue(m_Buffer, ref iterator, ref value);
                }
                else
                {
                    UnsafeHashMapBase<TKey, TValue>.TryAdd(m_Buffer, key, value, false, m_AllocatorLabel);
                }
            }
        }

        /// <summary>
        /// Reports whether memory for the container is allocated.
        /// </summary>
        /// <value>True if this container object's internal storage has been allocated.</value>
        /// <remarks>Note that the container storage is not created if you use the default constructor. You must specify
        /// at least an allocation type to construct a usable container.</remarks>
        public bool IsCreated => m_Buffer != null;

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
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
        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new UnsafeHashMapDisposeJob { Data = m_Buffer, Allocator = m_AllocatorLabel }.Schedule(inputDeps);
            m_Buffer = null;
            return jobHandle;
        }

        /// <summary>
        /// Retrive array of key from the container.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns></returns>
        public NativeArray<TKey> GetKeyArray(Allocator allocator)
        {
            var result = new NativeArray<TKey>(UnsafeHashMapData.GetCount(m_Buffer), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeHashMapData.GetKeyArray(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// Retreive array of values from the container.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns></returns>
        public NativeArray<TValue> GetValueArray(Allocator allocator)
        {
            var result = new NativeArray<TValue>(UnsafeHashMapData.GetCount(m_Buffer), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeHashMapData.GetValueArray(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// Retrieve key/value arrays.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns></returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(Allocator allocator)
        {
            var result = new NativeKeyValueArrays<TKey, TValue>(UnsafeHashMapData.GetCount(m_Buffer), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeHashMapData.GetKeyValueArrays(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// Returns parallel writer instance.
        /// </summary>
        /// <returns>Parallel writer instance.</returns>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;
            writer.m_ThreadIndex = 0;
            writer.m_Buffer = m_Buffer;
            return writer;
        }

        /// <summary>
        /// Implements parallel writer. Use AsParallelWriter to obtain it from container.
        /// </summary>
        [NativeContainerIsAtomicWriteOnly]
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
                    UnsafeHashMapData* data = m_Buffer;
                    return data->keyCapacity;
                }
            }

            /// <summary>
            /// Try adding an element with the specified key and value into the container. If the key already exist, the value won't be updated.
            /// </summary>
            /// <param name="key">The key of the element to add.</param>
            /// <param name="item">The value of the element to add.</param>
            /// <returns>Returns true if value is added into the container, otherwise returns false.</returns>
            public bool TryAdd(TKey key, TValue item)
            {
                Assert.IsTrue(m_ThreadIndex >= 0);
                return UnsafeHashMapBase<TKey, TValue>.TryAddAtomic(m_Buffer, key, item, m_ThreadIndex);
            }
        }

        /// <summary>
        /// Returns an IEnumerator interface for the container.
        /// </summary>
        /// <returns>An IEnumerator interface for the container.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator { m_Enumerator = new UnsafeHashMapDataEnumerator(m_Buffer) };
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
        public struct Enumerator : IEnumerator<KeyValue<TKey, TValue>>
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

            object IEnumerator.Current => throw new InvalidOperationException("Use IEnumerator<KeyValue<TKey, TValue>> to avoid boxing");
        }
    }

    [BurstCompile]
    internal unsafe struct UnsafeHashMapDisposeJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public UnsafeHashMapData* Data;
        public Allocator Allocator;

        public void Execute()
        {
            UnsafeHashMapData.DeallocateHashMap(Data, Allocator);
        }
    }

    sealed internal class UnsafeHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
#if !NET_DOTS
        UnsafeHashMap<TKey, TValue> m_Target;

        public UnsafeHashMapDebuggerTypeProxy(UnsafeHashMap<TKey, TValue> target)
        {
            m_Target = target;
        }

        public List<Pair<TKey, TValue>> Items
        {
            get
            {
                var result = new List<Pair<TKey, TValue>>();
                using (var kva = m_Target.GetKeyValueArrays(Allocator.Temp))
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

    /// <summary>
    /// <undoc />
    /// </summary>
    public unsafe struct UntypedUnsafeHashMap
    {
#pragma warning disable 169
        [NativeDisableUnsafePtrRestriction]
        UnsafeHashMapData* m_Buffer;
        Allocator m_AllocatorLabel;
#pragma warning restore 169
    }
}
