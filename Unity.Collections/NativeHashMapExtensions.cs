using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    /// <summary>
    /// NativeHashMap extensions.
    /// </summary>
    [BurstCompatible]
    public static class NativeHashMapExtensions
    {
#if !NET_DOTS // Tuple is not supported by TinyBCL
        /// <summary>
        /// Eliminates duplicates from every consecutive group of equivalent elements.
        /// </summary>
        /// <remarks>Array should be sorted before running unique operation.</remarks>
        /// <typeparam name="T">The type of values in the array.</typeparam>
        /// <param name="array">Array to perform unique operation on.</param>
        /// <returns>Number of unique elements in array.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
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
        /// Returns array populated with unique keys.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the container.</typeparam>
        /// <typeparam name="TValue">The type of the values in the container.</typeparam>
        /// <param name="container">This container.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns>Unique keys of the container.</returns>
        [NotBurstCompatible]
        public static (NativeArray<TKey>, int) GetUniqueKeyArray<TKey, TValue>(this UnsafeMultiHashMap<TKey, TValue> container, Allocator allocator)
            where TKey : struct, IEquatable<TKey>, IComparable<TKey>
            where TValue : struct
        {
            var result = container.GetKeyArray(allocator);
            result.Sort();
            int uniques = result.Unique();
            return (result, uniques);
        }

        /// <summary>
        /// Returns array populated with unique keys.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the container.</typeparam>
        /// <typeparam name="TValue">The type of the values in the container.</typeparam>
        /// <param name="container">This container.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns>Unique keys of the container.</returns>
        [NotBurstCompatible]
        public static (NativeArray<TKey>, int) GetUniqueKeyArray<TKey, TValue>(this NativeMultiHashMap<TKey, TValue> container, Allocator allocator)
            where TKey : struct, IEquatable<TKey>, IComparable<TKey>
            where TValue : struct
        {
            var result = container.GetKeyArray(allocator);
            result.Sort();
            int uniques = result.Unique();
            return (result, uniques);
        }

#endif

        /// <summary>
        /// Returns internal bucked data structure. Internal bucket structure is useful when creating custom
        /// jobs operating on container. Each bucket can be processed concurrently with other buckets, and all key/value
        /// pairs in each bucket must processed individually (in sequential order) by a single thread.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the container.</typeparam>
        /// <typeparam name="TValue">The type of the values in the container.</typeparam>
        /// <param name="container">This container.</param>
        /// <returns>Returns internal bucked data structure.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static unsafe UnsafeHashMapBucketData GetBucketData<TKey, TValue>(this NativeHashMap<TKey, TValue> container)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            return container.m_HashMapData.m_Buffer->GetBucketData();
        }

        /// <summary>
        /// Returns internal bucked data structure. Internal bucket structure is useful when creating custom
        /// jobs operating on container. Each bucket can be processed concurrently with other buckets, and all key/value
        /// pairs in each bucket must processed individually (in sequential order) by a single thread.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the container.</typeparam>
        /// <typeparam name="TValue">The type of the values in the container.</typeparam>
        /// <param name="container">This container.</param>
        /// <returns>Returns internal bucked data structure.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static unsafe UnsafeHashMapBucketData GetUnsafeBucketData<TKey, TValue>(this NativeMultiHashMap<TKey, TValue> container)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            return container.m_MultiHashMapData.m_Buffer->GetBucketData();
        }

        /// <summary>
        /// Removes all elements with the specified key from the container.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the container.</typeparam>
        /// <typeparam name="TValue">The type of the values in the container.</typeparam>
        /// <param name="container">This container.</param>
        /// <param name="key">The key of the element to remove.</param>
        /// <param name="value">The value of the element to remove.</param>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static void Remove<TKey, TValue>(this NativeMultiHashMap<TKey, TValue> container, TKey key, TValue value) where TKey : struct, IEquatable<TKey> where TValue : struct, IEquatable<TValue>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(container.m_Safety);
#endif
            container.m_MultiHashMapData.Remove(key, value);
        }
    }
}
