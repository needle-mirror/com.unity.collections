using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Jobs;
using UnityEngine.Assertions;

#pragma warning disable 618 // disable obsolete warnings

namespace Unity.Collections.LowLevel.Unsafe
{

    /// <summary>
    /// An unmanaged, resizable list, without any thread safety check features.
    /// </summary>
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}, IsEmpty = {IsEmpty}")]
    [DebuggerTypeProxy(typeof(UnsafePtrListDebugView))]
    [Obsolete("Untyped UnsafePtrList is deprecated, please use UnsafePtrList<T> instead. (RemovedAfter 2021-05-18)", false)]
    public unsafe struct UnsafePtrList
        : INativeDisposable
        , INativeList<IntPtr>
        , IEnumerable<IntPtr> // Used by collection initializers.
    {
        /// <summary>
        ///
        /// </summary>
        [NativeDisableUnsafePtrRestriction]
        public readonly void** Ptr;

        /// <summary>
        ///
        /// </summary>
        public readonly int length;

        /// <summary>
        ///
        /// </summary>
        public readonly int capacity;

        /// <summary>
        ///
        /// </summary>
        public readonly AllocatorManager.AllocatorHandle Allocator;

        /// <summary>
        ///
        /// </summary>
        public int Length { get { return length; } set { } }

        /// <summary>
        ///
        /// </summary>
        public int Capacity { get { return capacity; } set { } }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IntPtr this[int index]
        {
            get { return new IntPtr(Ptr[index]); }
            set { Ptr[index] = (void*)value; }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ref IntPtr ElementAt(int index)
        {
            return ref ((IntPtr*)Ptr)[index];
        }

        /// <summary>
        /// Constructs list as view into memory.
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="length"></param>
        public unsafe UnsafePtrList(void** ptr, int length)
        {
            Ptr = ptr;
            this.length = length;
            this.capacity = length;
            Allocator = AllocatorManager.None;
        }

        /// <summary>
        /// Constructs a new list using the specified type of memory allocation.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        /// <remarks>The list initially has a capacity of one. To avoid reallocating memory for the list, specify
        /// sufficient capacity up front.</remarks>
        public unsafe UnsafePtrList(int initialCapacity, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            Ptr = null;
            length = 0;
            capacity = 0;
            Allocator = AllocatorManager.None;

            var sizeOf = IntPtr.Size;
            this.ListData() = new UnsafeList(sizeOf, sizeOf, initialCapacity, allocator, options);
        }

        /// <summary>
        /// Constructs a new list using the specified type of memory allocation.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        /// <remarks>The list initially has a capacity of one. To avoid reallocating memory for the list, specify
        /// sufficient capacity up front.</remarks>
        public unsafe UnsafePtrList(int initialCapacity, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            Ptr = null;
            length = 0;
            capacity = 0;
            Allocator = AllocatorManager.None;

            var sizeOf = IntPtr.Size;
            this.ListData() = new UnsafeList(sizeOf, sizeOf, initialCapacity, allocator, options);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="length"></param>
        /// <returns>New initialized container.</returns>
        public static UnsafePtrList* Create(void** ptr, int length)
        {
            UnsafePtrList* listData = AllocatorManager.Allocate<UnsafePtrList>(AllocatorManager.Persistent);
            *listData = new UnsafePtrList(ptr, length);
            return listData;
        }

        /// <summary>
        /// Creates a new list with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        /// <returns>New initialized container.</returns>
        public static UnsafePtrList* Create(int initialCapacity, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            UnsafePtrList* listData = AllocatorManager.Allocate<UnsafePtrList>(allocator);
            *listData = new UnsafePtrList(initialCapacity, allocator, options);
            return listData;
        }

        /// <summary>
        /// Destroys list.
        /// </summary>
        /// <param name="listData">Container to destroy.</param>
        public static void Destroy(UnsafePtrList* listData)
        {
            UnsafeList.CheckNull(listData);
            var allocator = listData->ListData().Allocator.Value == AllocatorManager.Invalid.Value
                ? AllocatorManager.Persistent
                : listData->ListData().Allocator
            ;
            listData->Dispose();
            AllocatorManager.Free(allocator, listData);
        }

        /// <summary>
        /// Reports whether container is empty.
        /// </summary>
        /// <value>True if this container empty.</value>
        public bool IsEmpty => !IsCreated || Length == 0;

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
        public bool IsCreated => Ptr != null;

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
            this.ListData().Dispose();
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
        [NotBurstCompatible /* This is not burst compatible because of IJob's use of a static IntPtr. Should switch to IJobBurstSchedulable in the future */]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            return this.ListData().Dispose(inputDeps);
        }

        /// <summary>
        /// Clears the list.
        /// </summary>
        /// <remarks>List Capacity remains unchanged.</remarks>
        public void Clear()
        {
            this.ListData().Clear();
        }

        /// <summary>
        /// Changes the list length, resizing if necessary.
        /// </summary>
        /// <param name="length">The new length of the list.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        public void Resize(int length, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            this.ListData().Resize<IntPtr>(length, options);
        }

        /// <summary>
        /// Set the number of items that can fit in the list.
        /// </summary>
        /// <param name="capacity">The number of items that the list can hold before it resizes its internal storage.</param>
        public void SetCapacity(int capacity)
        {
            this.ListData().SetCapacity<IntPtr>(capacity);
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the container.
        /// </summary>
        public void TrimExcess()
        {
            this.ListData().TrimExcess<IntPtr>();
        }

        /// <summary>
        /// Searches for the specified element in list.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>The zero-based index of the first occurrence element if found, otherwise returns -1.</returns>
        public int IndexOf(void* value)
        {
            for (int i = 0; i < Length; ++i)
            {
                if (Ptr[i] == value) return i;
            }

            return -1;
        }

        /// <summary>
        /// Determines whether an element is in the list.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>True, if element is found.</returns>
        public bool Contains(void* value)
        {
            return IndexOf(value) != -1;
        }

        /// <summary>
        /// Adds an element to the list.
        /// </summary>
        /// <param name="value">The value to be added at the end of the list.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void AddNoResize(void* value)
        {
            this.ListData().AddNoResize((IntPtr)value);
        }

        /// <summary>
        /// Adds elements from a buffer to this list.
        /// </summary>
        /// <param name="ptr">A pointer to the buffer.</param>
        /// <param name="length">The number of elements to add to the list.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void AddRangeNoResize(void** ptr, int length)
        {
            this.ListData().AddRangeNoResize<IntPtr>(ptr, length);
        }

        /// <summary>
        /// Adds elements from a list to this list.
        /// </summary>
        /// <param name="list">Other container to copy elements from.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void AddRangeNoResize(UnsafePtrList list)
        {
            this.ListData().AddRangeNoResize<IntPtr>(list.Ptr, list.Length);
        }

        /// <summary>
        /// Adds an element to the list.
        /// </summary>
        /// <param name="value">The struct to be added at the end of the list.</param>
        public void Add(in IntPtr value)
        {
            this.ListData().Add(value);
        }

        /// <summary>
        /// Adds an element to the list.
        /// </summary>
        /// <param name="value">The struct to be added at the end of the list.</param>
        public void Add(void* value)
        {
            this.ListData().Add((IntPtr)value);
        }

        /// <summary>
        /// Adds elements from a buffer to this list.
        /// </summary>
        /// <param name="ptr">A pointer to the buffer.</param>
        /// <param name="length">The number of elements to add to the list.</param>
        public void AddRange(void* ptr, int length)
        {
            this.ListData().AddRange<IntPtr>(ptr, length);
        }

        /// <summary>
        /// Adds the elements of a UnsafePtrList to this list.
        /// </summary>
        /// <param name="list">Other container to copy elements from.</param>
        public void AddRange(UnsafePtrList list)
        {
            this.ListData().AddRange<IntPtr>(list.ListData());
        }

        /// <summary>
        /// Inserts a number of items into a container at a specified zero-based index.
        /// </summary>
        /// <param name="begin">The zero-based index at which the new elements should be inserted.</param>
        /// <param name="end">The zero-based index just after where the elements should be removed.</param>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        public void InsertRangeWithBeginEnd(int begin, int end)
        {
            this.ListData().InsertRangeWithBeginEnd<IntPtr>(begin, end);
        }

        /// <summary>
        /// Truncates the list by replacing the item at the specified index with the last item in the list. The list
        /// is shortened by one.
        /// </summary>
        /// <param name="index">The index of the item to delete.</param>
        public void RemoveAtSwapBack(int index)
        {
            this.ListData().RemoveAtSwapBack<IntPtr>(index);
        }

        /// <summary>
        /// Truncates the list by replacing the item at the specified index range with the items from the end the list. The list
        /// is shortened by number of elements in range.
        /// </summary>
        /// <param name="begin">The first index of the item to remove.</param>
        /// <param name="end">The index past-the-last item to remove.</param>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        public void RemoveRangeSwapBackWithBeginEnd(int begin, int end)
        {
            this.ListData().RemoveRangeSwapBackWithBeginEnd<IntPtr>(begin, end);
        }

        /// <summary>
        /// Truncates the list by removing the item at the specified index, and shifting all remaining items to replace removed item. The list
        /// is shortened by one.
        /// </summary>
        /// <param name="index">The index of the item to delete.</param>
        /// <remarks>
        /// This method of removing item is useful only in case when list is ordered and user wants to preserve order
        /// in list after removal In majority of cases is not important and user should use more performant `RemoveAtSwapBack`.
        /// </remarks>
        public void RemoveAt(int index)
        {
            this.ListData().RemoveAt<IntPtr>(index);
        }

        /// <summary>
        /// Truncates the list by removing the items at the specified index range, and shifting all remaining items to replace removed items. The list
        /// is shortened by number of elements in range.
        /// </summary>
        /// <param name="begin">The first index of the item to remove.</param>
        /// <param name="end">The index past-the-last item to remove.</param>
        /// <remarks>
        /// This method of removing item(s) is useful only in case when list is ordered and user wants to preserve order
        /// in list after removal In majority of cases is not important and user should use more performant `RemoveRangeSwapBackWithBeginEnd`.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        public void RemoveRangeWithBeginEnd(int begin, int end)
        {
            this.ListData().RemoveRangeWithBeginEnd<IntPtr>(begin, end);
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
        /// This method is not implemented. It will throw NotImplementedException if it is used.
        /// </summary>
        /// <remarks>Use Enumerator GetEnumerator() instead.</remarks>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<IntPtr> IEnumerable<IntPtr>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns parallel reader instance.
        /// </summary>
        /// <returns>Parallel reader instance.</returns>
        public ParallelReader AsParallelReader()
        {
            return new ParallelReader(Ptr, Length);
        }

        /// <summary>
        /// Implements parallel reader. Use AsParallelReader to obtain it from container.
        /// </summary>
        public unsafe struct ParallelReader
        {
            /// <summary>
            ///
            /// </summary>
            [NativeDisableUnsafePtrRestriction]
            public readonly void** Ptr;

            /// <summary>
            ///
            /// </summary>
            public readonly int Length;

            internal ParallelReader(void** ptr, int length)
            {
                Ptr = ptr;
                Length = length;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public int IndexOf(void* value)
            {
                for (int i = 0; i < Length; ++i)
                {
                    if (Ptr[i] == value) return i;
                }

                return -1;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public bool Contains(void* value)
            {
                return IndexOf(value) != -1;
            }
        }

        /// <summary>
        /// Returns parallel writer instance.
        /// </summary>
        /// <returns>Parallel writer instance.</returns>
        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter(Ptr, (UnsafeList*)UnsafeUtility.AddressOf(ref this));
        }

        /// <summary>
        ///
        /// </summary>
        public unsafe struct ParallelWriter
        {
            /// <summary>
            ///
            /// </summary>
            [NativeDisableUnsafePtrRestriction]
            public readonly void* Ptr;

            /// <summary>
            ///
            /// </summary>
            [NativeDisableUnsafePtrRestriction]
            public UnsafeList* ListData;

            internal unsafe ParallelWriter(void* ptr, UnsafeList* listData)
            {
                Ptr = ptr;
                ListData = listData;
            }

            /// <summary>
            /// Adds an element to the list.
            /// </summary>
            /// <param name="value">The value to be added at the end of the list.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            public void AddNoResize(void* value)
            {
                ListData->AddNoResize((IntPtr)value);
            }

            /// <summary>
            /// Adds elements from a buffer to this list.
            /// </summary>
            /// <param name="ptr">A pointer to the buffer.</param>
            /// <param name="length">The number of elements to add to the list.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            public void AddRangeNoResize(void** ptr, int length)
            {
                ListData->AddRangeNoResize<IntPtr>(ptr, length);
            }

            /// <summary>
            /// Adds elements from a list to this list.
            /// </summary>
            /// <param name="list">Other container to copy elements from.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            public void AddRangeNoResize(UnsafePtrList list)
            {
                ListData->AddRangeNoResize<IntPtr>(list.Ptr, list.Length);
            }
        }
    }

    internal static class UnsafePtrListExtensions
    {
        public static ref UnsafeList ListData(ref this UnsafePtrList from) => ref UnsafeUtility.As<UnsafePtrList, UnsafeList>(ref from);
    }

    internal sealed class UnsafePtrListDebugView
    {
        UnsafePtrList Data;

        public UnsafePtrListDebugView(UnsafePtrList data)
        {
            Data = data;
        }

        public unsafe IntPtr[] Items
        {
            get
            {
                IntPtr[] result = new IntPtr[Data.Length];

                for (var i = 0; i < result.Length; ++i)
                {
                    result[i] = (IntPtr)Data.Ptr[i];
                }

                return result;
            }
        }
    }
    [Obsolete("This storage will no longer be used. (RemovedAfter 2021-06-01)")]
    sealed class WordStorageDebugView
    {
        WordStorage m_wordStorage;

        public WordStorageDebugView(WordStorage wordStorage)
        {
            m_wordStorage = wordStorage;
        }

        public FixedString128[] Table
        {
            get
            {
                var table = new FixedString128[m_wordStorage.Entries];
                for (var i = 0; i < m_wordStorage.Entries; ++i)
                    m_wordStorage.GetFixedString(i, ref table[i]);
                return table;
            }
        }
    }

     [Obsolete("This storage will no longer be used. (RemovedAfter 2021-06-01)")]
    sealed class WordStorageStatic
    {
        private WordStorageStatic()
        {
        }
        public struct Thing
        {
            public WordStorage Data;
        }
        public static Thing Ref = default;
    }

    /// <summary>
    ///
    /// </summary>
    [Obsolete("This storage will no longer be used. (RemovedAfter 2021-06-01)")]
    [DebuggerTypeProxy(typeof(WordStorageDebugView))]
    public struct WordStorage
    {
        struct Entry
        {
            public int offset;
            public int length;
        }

        NativeArray<byte> buffer; // all the UTF-8 encoded bytes in one place
        NativeArray<Entry> entry; // one offset for each text in "buffer"
        NativeMultiHashMap<int, int> hash; // from string hash to table entry
        int chars; // bytes in buffer allocated so far
        int entries; // number of strings allocated so far

        /// <summary>
        ///
        /// </summary>
        [NotBurstCompatible]
        public static ref WordStorage Instance
        {
            get
            {
                Initialize();
                return ref WordStorageStatic.Ref.Data;
            }
        }

        const int kMaxEntries = 16 << 10;
        const int kMaxChars = kMaxEntries * 128;

        /// <summary>
        ///
        /// </summary>
        public const int kMaxCharsPerEntry = 4096;

        /// <summary>
        ///
        /// </summary>
        public int Entries => entries;

        [NotBurstCompatible]
        public static void Initialize()
        {
            if (WordStorageStatic.Ref.Data.buffer.IsCreated)
                return;
            WordStorageStatic.Ref.Data.buffer = new NativeArray<byte>(kMaxChars, Allocator.Persistent);
            WordStorageStatic.Ref.Data.entry = new NativeArray<Entry>(kMaxEntries, Allocator.Persistent);
            WordStorageStatic.Ref.Data.hash = new NativeMultiHashMap<int, int>(kMaxEntries, Allocator.Persistent);
            Clear();
#if !UNITY_DOTSRUNTIME
            // Free storage on domain unload, which happens when iterating on the Entities module a lot.
            AppDomain.CurrentDomain.DomainUnload += (_, __) => { Shutdown(); };
#endif
        }

        /// <summary>
        ///
        /// </summary>
        [NotBurstCompatible]
        public static void Shutdown()
        {
            if (!WordStorageStatic.Ref.Data.buffer.IsCreated)
                return;
            WordStorageStatic.Ref.Data.buffer.Dispose();
            WordStorageStatic.Ref.Data.entry.Dispose();
            WordStorageStatic.Ref.Data.hash.Dispose();
            WordStorageStatic.Ref.Data = default;
        }

        [NotBurstCompatible]
        public static void Clear()
        {
            Initialize();
            WordStorageStatic.Ref.Data.chars = 0;
            WordStorageStatic.Ref.Data.entries = 0;
            WordStorageStatic.Ref.Data.hash.Clear();
            var temp = new FixedString32();
            WordStorageStatic.Ref.Data.GetOrCreateIndex(ref temp); // make sure that Index=0 means empty string
        }

        /// <summary>
        ///
        /// </summary>
        [NotBurstCompatible]
        public static void Setup()
        {
            Clear();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public unsafe void GetFixedString<T>(int index, ref T temp)
        where T : IUTF8Bytes, INativeList<byte>
        {
            Assert.IsTrue(index < entries);
            var e = entry[index];
            Assert.IsTrue(e.length <= kMaxCharsPerEntry);
            temp.Length = e.length;
            UnsafeUtility.MemCpy(temp.GetUnsafePtr(), (byte*)buffer.GetUnsafePtr() + e.offset, temp.Length);
        }

        public int GetIndexFromHashAndFixedString<T>(int h, ref T temp)
        where T : IUTF8Bytes, INativeList<byte>
        {
            Assert.IsTrue(temp.Length <= kMaxCharsPerEntry); // about one printed page of text
            int itemIndex;
            NativeMultiHashMapIterator<int> iter;
            if (hash.TryGetFirstValue(h, out itemIndex, out iter))
            {
                do
                {
                    var e = entry[itemIndex];
                    Assert.IsTrue(e.length <= kMaxCharsPerEntry);
                    if (e.length == temp.Length)
                    {
                        int matches;
                        for (matches = 0; matches < e.length; ++matches)
                            if (temp[matches] != buffer[e.offset + matches])
                                break;
                        if (matches == temp.Length)
                            return itemIndex;
                    }
                } while (hash.TryGetNextValue(out itemIndex, ref iter));
            }
            return -1;
        }

        public bool Contains<T>(ref T value)
        where T : IUTF8Bytes, INativeList<byte>
        {
            int h = value.GetHashCode();
            return GetIndexFromHashAndFixedString(h, ref value) != -1;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [NotBurstCompatible]
        public unsafe bool Contains(string value)
        {
            FixedString512 temp = value;
            return Contains(ref temp);
        }

        public int GetOrCreateIndex<T>(ref T value)
        where T : IUTF8Bytes, INativeList<byte>
        {
            int h = value.GetHashCode();
            var itemIndex = GetIndexFromHashAndFixedString(h, ref value);
            if (itemIndex != -1)
                return itemIndex;
            Assert.IsTrue(entries < kMaxEntries);
            Assert.IsTrue(chars + value.Length <= kMaxChars);
            var o = chars;
            var l = (ushort)value.Length;
            for (var i = 0; i < l; ++i)
                buffer[chars++] = value[i];
            entry[entries] = new Entry { offset = o, length = l };
            hash.Add(h, entries);
            return entries++;
        }
    }


    /// <summary>
    ///
    /// </summary>
    /// <remarks>
    /// A "Words" is an integer that refers to 4,096 or fewer chars of UTF-16 text in a global storage blob.
    /// Each should refer to *at most* about one printed page of text.
    ///
    /// If you need more text, consider using one Words struct for each printed page's worth.
    ///
    /// Each Words instance that you create is stored in a single, internally-managed WordStorage object,
    /// which can hold up to 16,384 Words entries. Once added, the entries in WordStorage cannot be modified
    /// or removed.
    /// </remarks>
    [Obsolete("This storage will no longer be used. (RemovedAfter 2021-06-01)")]
    public struct Words
    {
        int Index;

        public void ToFixedString<T>(ref T value)
        where T : IUTF8Bytes, INativeList<byte>
        {
            WordStorage.Instance.GetFixedString(Index, ref value);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            FixedString512 temp = default;
            ToFixedString(ref temp);
            return temp.ToString();
        }

        public void SetFixedString<T>(ref T value)
        where T : IUTF8Bytes, INativeList<byte>
        {
            Index = WordStorage.Instance.GetOrCreateIndex(ref value);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        public unsafe void SetString(string value)
        {
            FixedString512 temp = value;
            SetFixedString(ref temp);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <remarks>
    /// A "NumberedWords" is a "Words", plus possibly a string of leading zeroes, followed by
    /// possibly a positive integer.
    /// The zeroes and integer aren't stored centrally as a string, they're stored as an int.
    /// Therefore, 1,000,000 items with names from FooBarBazBifBoo000000 to FooBarBazBifBoo999999
    /// Will cost 8MB + a single copy of "FooBarBazBifBoo", instead of ~48MB.
    /// They say that this is a thing, too.
    /// </remarks>

    [Obsolete("This storage will no longer be used. (RemovedAfter 2021-06-01)")]
    public struct NumberedWords
    {
        int Index;
        int Suffix;

        const int kPositiveNumericSuffixShift = 0;
        const int kPositiveNumericSuffixBits = 29;
        const int kMaxPositiveNumericSuffix = (1 << kPositiveNumericSuffixBits) - 1;
        const int kPositiveNumericSuffixMask = (1 << kPositiveNumericSuffixBits) - 1;

        const int kLeadingZeroesShift = 29;
        const int kLeadingZeroesBits = 3;
        const int kMaxLeadingZeroes = (1 << kLeadingZeroesBits) - 1;
        const int kLeadingZeroesMask = (1 << kLeadingZeroesBits) - 1;

        int LeadingZeroes
        {
            get => (Suffix >> kLeadingZeroesShift) & kLeadingZeroesMask;
            set
            {
                Suffix &= ~(kLeadingZeroesMask << kLeadingZeroesShift);
                Suffix |= (value & kLeadingZeroesMask) << kLeadingZeroesShift;
            }
        }

        int PositiveNumericSuffix
        {
            get => (Suffix >> kPositiveNumericSuffixShift) & kPositiveNumericSuffixMask;
            set
            {
                Suffix &= ~(kPositiveNumericSuffixMask << kPositiveNumericSuffixShift);
                Suffix |= (value & kPositiveNumericSuffixMask) << kPositiveNumericSuffixShift;
            }
        }

        bool HasPositiveNumericSuffix => PositiveNumericSuffix != 0;

        [NotBurstCompatible]
        string NewString(char c, int count)
        {
            char[] temp = new char[count];
            for (var i = 0; i < count; ++i)
                temp[i] = c;
            return new string(temp, 0, count);
        }

        [NotBurstCompatible]
        public int ToFixedString<T>(ref T result)
        where T : IUTF8Bytes, INativeList<byte>
        {
            unsafe
            {
                var positiveNumericSuffix = PositiveNumericSuffix;
                var leadingZeroes = LeadingZeroes;

                WordStorage.Instance.GetFixedString(Index, ref result);
                if(positiveNumericSuffix == 0 && leadingZeroes == 0)
                    return 0;

                // print the numeric suffix, if any, backwards, as ASCII, to a little buffer.
                const int maximumDigits = kMaxLeadingZeroes + 10;
                var buffer = stackalloc byte[maximumDigits];
                var firstDigit = maximumDigits;
                while(positiveNumericSuffix > 0)
                {
                    buffer[--firstDigit] = (byte)('0' + positiveNumericSuffix % 10);
                    positiveNumericSuffix /= 10;
                }
                while(leadingZeroes-- > 0)
                    buffer[--firstDigit] = (byte)'0';

                // make space in the output for leading zeroes if any, followed by the positive numeric index if any.
                var dest = result.GetUnsafePtr() + result.Length;
                result.Length += maximumDigits - firstDigit;
                while(firstDigit < maximumDigits)
                    *dest++ = buffer[firstDigit++];
                return 0;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        [NotBurstCompatible]
        public override string ToString()
        {
            FixedString512 temp = default;
            ToFixedString(ref temp);
            return temp.ToString();
        }

        bool IsDigit(byte b)
        {
            return b >= '0' && b <= '9';
        }

        [NotBurstCompatible]
        public void SetString<T>(ref T value)
        where T : IUTF8Bytes, INativeList<byte>
        {
            int beginningOfDigits = value.Length;

            // as long as there are digits at the end,
            // look back for more digits.

            while (beginningOfDigits > 0 && IsDigit(value[beginningOfDigits - 1]))
                --beginningOfDigits;

            // as long as the first digit is a zero, it's not the beginning of the positive integer - it's a leading zero.

            var beginningOfPositiveNumericSuffix = beginningOfDigits;
            while (beginningOfPositiveNumericSuffix < value.Length && value[beginningOfPositiveNumericSuffix] == '0')
                ++beginningOfPositiveNumericSuffix;

            // now we know where the leading zeroes begin, and then where the positive integer begins after them.
            // but if there are too many leading zeroes to encode, the excess ones become part of the string.

            var leadingZeroes = beginningOfPositiveNumericSuffix - beginningOfDigits;
            if (leadingZeroes > kMaxLeadingZeroes)
            {
                var excessLeadingZeroes = leadingZeroes - kMaxLeadingZeroes;
                beginningOfDigits += excessLeadingZeroes;
                leadingZeroes -= excessLeadingZeroes;
            }

            // if there is a positive integer after the zeroes, here's where we compute it and store it for later.

            PositiveNumericSuffix = 0;
            {
                int number = 0;
                for (var i = beginningOfPositiveNumericSuffix; i < value.Length; ++i)
                {
                    number *= 10;
                    number += value[i] - '0';
                }

                // an intrepid user may attempt to encode a positive integer with 20 digits or something.
                // they are rewarded with a string that is encoded wholesale without any optimizations.

                if (number <= kMaxPositiveNumericSuffix)
                    PositiveNumericSuffix = number;
                else
                {
                    beginningOfDigits = value.Length;
                    leadingZeroes = 0; // and your dog Toto, too.
                }
            }

            // set the leading zero count in the Suffix member.

            LeadingZeroes = leadingZeroes;

            // truncate the string, if there were digits at the end that we encoded.
            var truncated = value;
            int length = truncated.Length;
            if (beginningOfDigits != truncated.Length)
                truncated.Length = beginningOfDigits;

            // finally, set the string to its index in the global string blob thing.

            unsafe
            {
                Index = WordStorage.Instance.GetOrCreateIndex(ref truncated);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        [NotBurstCompatible]
        public void SetString(string value)
        {
            FixedString512 temp = value;
            SetString(ref temp);
        }
    }
}
