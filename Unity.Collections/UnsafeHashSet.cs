using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Jobs;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// An unordered, expandable set of unique values.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerTypeProxy(typeof(UnsafeHashSetDebuggerTypeProxy<>))]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
    public unsafe struct UnsafeHashSet<T>
        : INativeDisposable
        , IEnumerable<T> // Used by collection initializers.
        where T : unmanaged, IEquatable<T>
    {
        internal HashMapHelper<T> m_Data;

        /// <summary>
        /// Initializes and returns an instance of NativeParallelHashSet.
        /// </summary>
        /// <param name="capacity">The number of values that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
        public UnsafeHashSet(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_Data = default;
            m_Data.Init(capacity, 0, 256, allocator);
        }

        /// <summary>
        /// Whether this set is empty.
        /// </summary>
        /// <value>True if this set is empty or if the set has not been constructed.</value>
        public bool IsEmpty
        {
            get
            {
                if (!IsCreated)
                {
                    return true;
                }

                return m_Data.IsEmpty;
            }
        }

        /// <summary>
        /// Returns the current number of values in this set.
        /// </summary>
        /// <returns>The current number of values in this set.</returns>
        public int Count
        {
            get
            {
                return m_Data.Count;
            }
        }

        /// <summary>
        /// The number of values that fit in the current allocation.
        /// </summary>
        /// <value>The number of values that fit in the current allocation.</value>
        /// <param name="value">A new capacity. Must be larger than current capacity.</param>
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
        /// Whether this set has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this set has been allocated (and not yet deallocated).</value>
        public bool IsCreated => m_Data.IsCreated;

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
            m_Data.Dispose();
        }

        /// <summary>
        /// Creates and schedules a job that will dispose this set.
        /// </summary>
        /// <param name="inputDeps">A job handle. The newly scheduled job will depend upon this handle.</param>
        /// <returns>The handle of a new job that will dispose this set.</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new UnsafeDisposeJob
            {
                Ptr = m_Data.Ptr,
                Allocator = m_Data.Allocator,

            }.Schedule(inputDeps);

            m_Data.Ptr = null;

            return jobHandle;
        }

        /// <summary>
        /// Removes all values.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            m_Data.Clear();
        }

        /// <summary>
        /// Adds a new value (unless it is already present).
        /// </summary>
        /// <param name="item">The value to add.</param>
        /// <returns>True if the value was not already present.</returns>
        public bool Add(T item)
        {
            m_Data.UpdateCapacity();
            return -1 != m_Data.TryAdd(item);
        }

        /// <summary>
        /// Removes a particular value.
        /// </summary>
        /// <param name="item">The value to remove.</param>
        /// <returns>True if the value was present.</returns>
        public bool Remove(T item)
        {
            return -1 != m_Data.TryRemove(item);
        }

        /// <summary>
        /// Returns true if a particular value is present.
        /// </summary>
        /// <param name="item">The value to check for.</param>
        /// <returns>True if the value was present.</returns>
        public bool Contains(T item)
        {
            return -1 != m_Data.Find(item);
        }

        /// <summary>
        /// Returns an array with a copy of this set's values (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of the set's values.</returns>
        public NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator)
        {
            return m_Data.GetKeyArray(allocator);
        }

        /// <summary>
        /// Returns an enumerator over the values of this set.
        /// </summary>
        /// <returns>An enumerator over the values of this set.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator
            {
                m_Data = m_Data,
                m_Index = -1,
            };
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
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
        /// An enumerator over the values of a set.
        /// </summary>
        /// <remarks>
        /// In an enumerator's initial state, <see cref="Current"/> is invalid.
        /// The first <see cref="MoveNext"/> call advances the enumerator to the first value.
        /// </remarks>
        public struct Enumerator : IEnumerator<T>
        {
            internal HashMapHelper<T> m_Data;
            internal int m_Index;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next value.
            /// </summary>
            /// <returns>True if `Current` is valid to read after the call.</returns>
            public bool MoveNext()
            {
                m_Index = m_Data.FindNext(m_Index + 1);
                return m_Index != -1;
            }

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset()
            {
                m_Index = -1;
            }

            /// <summary>
            /// The current value.
            /// </summary>
            /// <value>The current value.</value>
            public T Current
            {
                get
                {
                    return m_Data.GetKeyAt(m_Index);
                }
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator in the container.
            /// </summary>
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
        /// A read-only alias for the value of a UnsafeHashSet. Does not have its own allocated storage.
        /// </summary>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public struct ReadOnly
            : IEnumerable<T>
        {
            internal HashMapHelper<T> m_Data;

            internal ReadOnly(ref HashMapHelper<T> data)
            {
                m_Data = data;
            }

            /// <summary>
            /// Whether this hash set is empty.
            /// </summary>
            /// <value>True if this hash set is empty or if the set has not been constructed.</value>
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
            /// Returns true if a particular value is present.
            /// </summary>
            /// <param name="item">The item to look up.</param>
            /// <returns>True if the item was present.</returns>
            public bool Contains(T item)
            {
                return -1 != m_Data.Find(item);
            }

            /// <summary>
            /// Returns an array with a copy of this set's values (in no particular order).
            /// </summary>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>An array with a copy of the set's values.</returns>
            public NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator)
            {
                return m_Data.GetKeyArray(allocator);
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
                return new Enumerator { m_Data = m_Data, m_Index = -1 };
            }

            /// <summary>
            /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
            /// </summary>
            /// <returns>Throws NotImplementedException.</returns>
            /// <exception cref="NotImplementedException">Method is not implemented.</exception>
            IEnumerator<T> IEnumerable<T>.GetEnumerator()
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
    }

    sealed internal class UnsafeHashSetDebuggerTypeProxy<T>
        where T : unmanaged, IEquatable<T>
    {
#if !NET_DOTS
        UnsafeHashSet<T> Data;

        public UnsafeHashSetDebuggerTypeProxy(UnsafeHashSet<T> data)
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
