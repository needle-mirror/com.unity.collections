using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Internal;

namespace Unity.Collections
{
    /// <summary>
    /// Set of values.
    /// </summary>
    /// <typeparam name="T">The type of the values in the container.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerTypeProxy(typeof(NativeHashSetDebuggerTypeProxy<>))]
    [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
    public unsafe struct NativeHashSet<T>
        : INativeDisposable
        , IEnumerable<T> // Used by collection initializers.
        where T : unmanaged, IEquatable<T>
    {
        internal NativeHashMap<T, bool> m_Data;

        /// <summary>
        /// Constructs a new container with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="capacity">The initial capacity of the container. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        public NativeHashSet(int capacity, Allocator allocator)
        {
            m_Data = new NativeHashMap<T, bool>(capacity, allocator);
        }

        /// <summary>
        /// Reports whether container is empty.
        /// </summary>
        /// <value>True if this container empty.</value>
        public bool IsEmpty => m_Data.IsEmpty;

        /// <summary>
        /// The current number of items in the container.
        /// </summary>
        /// <returns>The item count.</returns>
        public int Count() => m_Data.Count();

        /// <summary>
        /// The number of items that can fit in the container.
        /// </summary>
        /// <value>The number of items that the container can hold before it resizes its internal storage.</value>
        /// <remarks>Capacity specifies the number of items the container can currently hold. You can change Capacity
        /// to fit more or fewer items. Changing Capacity creates a new array of the specified size, copies the
        /// old array to the new one, and then deallocates the original array memory.</remarks>
        public int Capacity { get => m_Data.Capacity; set => m_Data.Capacity = value; }

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
        /// Accessing the data of a native container that has been disposed throws a <see cref='InvalidOperationException'/> exception.
        /// </remarks>
        public bool IsCreated => m_Data.IsCreated;

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
        /// </summary>
        public void Dispose() => m_Data.Dispose();

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
        public JobHandle Dispose(JobHandle inputDeps) => m_Data.Dispose(inputDeps);

        /// <summary>
        /// Clears the container.
        /// </summary>
        /// <remarks>Containers capacity remains unchanged.</remarks>
        public void Clear() => m_Data.Clear();

        /// <summary>
        /// Add the specified element into the container. If the specified element already exists in the container it returns false.
        /// </summary>
        /// <param name="item">The element to add to the container.</param>
        /// <returns>Returns true if the specified element is added into the container, otherwise returns false.</returns>
        public bool Add(T item) => m_Data.TryAdd(item, false);

        /// <summary>
        /// Removes the specified element from the container.
        /// </summary>
        /// <param name="item">The element to remove from the container.</param>
        /// <returns>Returns true if the specified element was removed from the container, otherwise returns false indicating the specified element wasn't in the container.</returns>
        public bool Remove(T item) => m_Data.Remove(item);

        /// <summary>
        /// Determines whether an element is in the container.
        /// </summary>
        /// <param name="item">The element to locate in the container.</param>
        /// <returns>Returns true if the specified element is inside the container, otherwise returns false indicating the specified element wasn't in the container.</returns>
        public bool Contains(T item) => m_Data.ContainsKey(item);

        /// <summary>
        /// Returns array populated with elements from the container.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns>Array of elements of the container.</returns>
        public NativeArray<T> ToNativeArray(Allocator allocator) => m_Data.GetKeyArray(allocator);

        /// <summary>
        /// Returns parallel writer instance.
        /// </summary>
        /// <returns>Parallel writer instance.</returns>
        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter { m_Data = m_Data.AsParallelWriter() };
        }

        /// <summary>
        /// Implements parallel writer. Use AsParallelWriter to obtain it from container.
        /// </summary>
        [NativeContainerIsAtomicWriteOnly]
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public struct ParallelWriter
        {
            internal NativeHashMap<T, bool>.ParallelWriter m_Data;

            /// <summary>
            /// The number of items that can fit in the container.
            /// </summary>
            /// <value>The number of items that the container can hold before it resizes its internal storage.</value>
            /// <remarks>Capacity specifies the number of items the container can currently hold.</remarks>
            public int Capacity => m_Data.Capacity;

            /// <summary>
            /// Add the specified element into the container. If the specified element already exists in the container it returns false.
            /// </summary>
            /// <param name="item">The element to add to the container.</param>
            /// <returns>Returns true if the specified element is added into the container, otherwise returns false.</returns>
            public bool Add(T item) => m_Data.TryAdd(item, false);
        }

        /// <summary>
        /// Returns an IEnumerator interface for the container.
        /// </summary>
        /// <returns>An IEnumerator interface for the container.</returns>
        public Enumerator GetEnumerator()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Data.m_Safety);
            var ash = m_Data.m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref ash);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(ash, true);
#endif
            return new Enumerator
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = ash,
#endif
                m_Enumerator = new UnsafeHashMapDataEnumerator(m_Data.m_HashMapData.m_Buffer),
            };
        }

        /// <summary>
        /// This method is not implemented. It will throw NotImplementedException if it is used.
        /// </summary>
        /// <remarks>Use Enumerator GetEnumerator() instead.</remarks>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
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
        public struct Enumerator : IEnumerator<T>
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
            public bool MoveNext()
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
            public T Current
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                    return m_Enumerator.GetCurrentKey<T>();
                }
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator in the container.
            /// </summary>
            object IEnumerator.Current => Current;
        }
    }

    sealed internal class NativeHashSetDebuggerTypeProxy<T>
        where T : unmanaged, IEquatable<T>
    {
#if !NET_DOTS
        NativeHashSet<T> Data;

        public NativeHashSetDebuggerTypeProxy(NativeHashSet<T> data)
        {
            Data = data;
        }

        public List<T> Items
        {
            get
            {
                var result = new List<T>();
                using (var keys = Data.ToNativeArray(Allocator.Temp))
                {
                    for (var k = 0; k < keys.Length; ++k)
                    {
                        result.Add(keys[k]);
                    }
                }

                return result;
            }
        }
#endif
    }
}
