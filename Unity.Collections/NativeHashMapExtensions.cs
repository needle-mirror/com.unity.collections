using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    /// <summary>
    /// NativeHashMap extensions.
    /// </summary>
    public static class NativeHashMapExtensions
    {
#if !NET_DOTS
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static int Unique<T>(this NativeArray<T> array)
            where T : struct, IEquatable<T>
        {
            if (array.Length == 0)
            {
                return 0;
            }

            int first = 0;
            int last = array.Length;
            var result = first;
            while (++first != last)
            {
                if (!array[result].Equals(array[first]))
                {
                    array[++result] = array[first];
                }
            }

            return ++result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the container.</typeparam>
        /// <typeparam name="TValue">The type of the values in the container.</typeparam>
        /// <param name="hashMap"></param>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public static (NativeArray<TKey>, int) GetUniqueKeyArray<TKey, TValue>(this NativeMultiHashMap<TKey, TValue> hashMap, Allocator allocator)
            where TKey : struct, IEquatable<TKey>, IComparable<TKey>
            where TValue : struct
        {
            var withDuplicates = hashMap.GetKeyArray(allocator);
            withDuplicates.Sort();
            int uniques = withDuplicates.Unique();
            return (withDuplicates, uniques);
        }

#endif

        /// <summary>
        /// Returns internal bucked data structure. Internal bucket structure is useful when creating custom
        /// jobs operating on container. Each bucket can be processed concurrently with other buckets, and all key/value
        /// pairs in each bucket must processed individually (in sequential order) by a single thread.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the container.</typeparam>
        /// <typeparam name="TValue">The type of the values in the container.</typeparam>
        /// <param name="hashMap"></param>
        /// <returns>Returns internal bucked data structure.</returns>
        public static unsafe UnsafeHashMapBucketData GetBucketData<TKey, TValue>(this NativeHashMap<TKey, TValue> hashMap)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            return hashMap.m_HashMapData.m_Buffer->GetBucketData();
        }

        /// <summary>
        /// Returns internal bucked data structure. Internal bucket structure is useful when creating custom
        /// jobs operating on container. Each bucket can be processed concurrently with other buckets, and all key/value
        /// pairs in each bucket must processed individually (in sequential order) by a single thread.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the container.</typeparam>
        /// <typeparam name="TValue">The type of the values in the container.</typeparam>
        /// <param name="multiHashMap">This container.</param>
        /// <returns>Returns internal bucked data structure.</returns>
        public static unsafe UnsafeHashMapBucketData GetUnsafeBucketData<TKey, TValue>(this NativeMultiHashMap<TKey, TValue> multiHashMap)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            return multiHashMap.m_MultiHashMapData.m_Buffer->GetBucketData();
        }

        /// <summary>
        /// Removes all elements with the specified key from the container.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the container.</typeparam>
        /// <typeparam name="TValue">The type of the values in the container.</typeparam>
        /// <param name="multiHashMap">This container.</param>
        /// <param name="key">The key of the element to remove.</param>
        /// <param name="value">The value of the element to remove.</param>
        public static void Remove<TKey, TValue>(this NativeMultiHashMap<TKey, TValue> multiHashMap, TKey key, TValue value) where TKey : struct, IEquatable<TKey> where TValue : struct, IEquatable<TValue>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(multiHashMap.m_Safety);
#endif
            multiHashMap.m_MultiHashMapData.Remove(key, value);
        }
    }
}
