using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

#pragma warning disable 618 // disable obsolete warnings

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// An unmanaged, untyped, resizable list, without any thread safety check features.
    /// </summary>
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}, IsEmpty = {IsEmpty}")]
    [StructLayout(LayoutKind.Sequential)]
    [Obsolete("Untyped UnsafeList is deprecated, please use UnsafeList<T> instead. (RemovedAfter 2021-05-18)", false)]
    public unsafe struct UnsafeList
        : INativeDisposable
    {
        /// <summary>
        /// </summary>
        [NativeDisableUnsafePtrRestriction]
        public void* Ptr;

        /// <summary>
        /// </summary>
        public int Length;

        public readonly int unused;

        /// <summary>
        /// </summary>
        public int Capacity;

        /// <summary>
        /// </summary>
        public AllocatorManager.AllocatorHandle Allocator;

        /// <summary>
        /// Constructs a new container with type of memory allocation.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <remarks>The list initially has a capacity of one. To avoid reallocating memory for the list, specify
        /// sufficient capacity up front.</remarks>
        public UnsafeList(Allocator allocator) : this()
        {
            Ptr = null;
            Length = 0;
            Capacity = 0;
            Allocator = allocator;
        }

        /// <summary>
        /// Constructs container as view into memory.
        /// </summary>
        /// <param name="ptr">Pointer to data.</param>
        /// <param name="length">Lenght of data in bytes.</param>
        public UnsafeList(void* ptr, int length) : this()
        {
            Ptr = ptr;
            Length = length;
            Capacity = length;
            Allocator = Collections.Allocator.None;
        }

        internal void Initialize<U>(int sizeOf, int alignOf, int initialCapacity, ref U allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) where U : unmanaged, AllocatorManager.IAllocator
        {
            Allocator = allocator.Handle;
            Ptr = null;
            Length = 0;
            Capacity = 0;

            if (initialCapacity != 0)
            {
                SetCapacity(ref allocator, sizeOf, alignOf, initialCapacity);
            }

            if (options == NativeArrayOptions.ClearMemory
                && Ptr != null)
            {
                UnsafeUtility.MemClear(Ptr, Capacity * sizeOf);
            }
        }

        internal static UnsafeList New<U>(int sizeOf, int alignOf, int initialCapacity, ref U allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) where U : unmanaged, AllocatorManager.IAllocator
        {
            var temp = new UnsafeList();
            temp.Initialize(sizeOf, alignOf, initialCapacity, ref allocator, options);
            return temp;
        }

        /// <summary>
        /// Constructs a new container with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="sizeOf">Size of element.</param>
        /// <param name="alignOf">Alignment of element.</param>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        public UnsafeList(int sizeOf, int alignOf, int initialCapacity, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) : this()
        {
            this = default;
            Initialize(sizeOf, alignOf, initialCapacity, ref allocator, options);
        }

        /// <summary>
        /// Constructs a new container with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="sizeOf">Size of element.</param>
        /// <param name="alignOf">Alignment of element.</param>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        public UnsafeList(int sizeOf, int alignOf, int initialCapacity, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) : this()
        {
            Allocator = allocator;
            Ptr = null;
            Length = 0;
            Capacity = 0;

            if (initialCapacity != 0)
            {
                SetCapacity(sizeOf, alignOf, initialCapacity);
            }

            if (options == NativeArrayOptions.ClearMemory
                && Ptr != null)
            {
                UnsafeUtility.MemClear(Ptr, Capacity * sizeOf);
            }
        }

        /// <summary>
        /// Creates a new container with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="sizeOf">Size of element.</param>
        /// <param name="alignOf">Alignment of element.</param>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        /// <returns>New initialized container.</returns>
        public static UnsafeList* Create(int sizeOf, int alignOf, int initialCapacity, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            var handle = (AllocatorManager.AllocatorHandle)allocator;
            UnsafeList* listData = AllocatorManager.Allocate<UnsafeList>(handle);
            UnsafeUtility.MemClear(listData, UnsafeUtility.SizeOf<UnsafeList>());

            listData->Allocator = allocator;

            if (initialCapacity != 0)
            {
                listData->SetCapacity(sizeOf, alignOf, initialCapacity);
            }

            if (options == NativeArrayOptions.ClearMemory
                && listData->Ptr != null)
            {
                UnsafeUtility.MemClear(listData->Ptr, listData->Capacity * sizeOf);
            }

            return listData;
        }

        internal static UnsafeList* Create<U>(int sizeOf, int alignOf, int initialCapacity, ref U allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) where U : unmanaged, AllocatorManager.IAllocator
        {
            UnsafeList* listData = allocator.Allocate(default(UnsafeList), 1);
            UnsafeUtility.MemClear(listData, UnsafeUtility.SizeOf<UnsafeList>());

            listData->Allocator = allocator.Handle;

            if (initialCapacity != 0)
            {
                listData->SetCapacity(ref allocator, sizeOf, alignOf, initialCapacity);
            }

            if (options == NativeArrayOptions.ClearMemory
                && listData->Ptr != null)
            {
                UnsafeUtility.MemClear(listData->Ptr, listData->Capacity * sizeOf);
            }

            return listData;
        }

        internal static void Destroy<U>(UnsafeList* listData, ref U allocator, int sizeOf, int alignOf) where U : unmanaged, AllocatorManager.IAllocator
        {
            CheckNull(listData);
            listData->Dispose(ref allocator, sizeOf, alignOf);
            allocator.Free(listData, UnsafeUtility.SizeOf<UnsafeList>(), UnsafeUtility.AlignOf<UnsafeList>(), 1);
        }

        /// <summary>
        /// Destroys container.
        /// </summary>
        /// <param name="listData">Container to destroy.</param>
        public static void Destroy(UnsafeList* listData)
        {
            CheckNull(listData);
            var allocator = listData->Allocator;
            listData->Dispose();
            AllocatorManager.Free(allocator, listData);
        }

        /// <summary>
        /// Reports whether container is empty.
        /// </summary>
        /// <value>True if this string has no characters or if the container has not been constructed.</value>
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
            if (CollectionHelper.ShouldDeallocate(Allocator))
            {
                AllocatorManager.Free(Allocator, Ptr);
                Allocator = AllocatorManager.Invalid;
            }

            Ptr = null;
            Length = 0;
            Capacity = 0;
        }

        internal void Dispose<U>(ref U allocator, int sizeOf, int alignOf) where U : unmanaged, AllocatorManager.IAllocator
        {
            allocator.Free(Ptr, sizeOf, alignOf, Length);
            Ptr = null;
            Length = 0;
            Capacity = 0;
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
            if (CollectionHelper.ShouldDeallocate(Allocator))
            {
                var jobHandle = new UnsafeDisposeJob { Ptr = Ptr, Allocator = (Allocator)Allocator.Value }.Schedule(inputDeps);

                Ptr = null;
                Allocator = AllocatorManager.Invalid;

                return jobHandle;
            }

            Ptr = null;

            return inputDeps;
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        /// <remarks>The container capacity remains unchanged.</remarks>
        public void Clear()
        {
            Length = 0;
        }

        /// <summary>
        /// Changes the list length, resizing if necessary.
        /// </summary>
        /// <param name="sizeOf">Size of element.</param>
        /// <param name="alignOf">Alignment of element.</param>
        /// <param name="length">The new length of the list.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        public void Resize(int sizeOf, int alignOf, int length, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            var oldLength = Length;

            if (length > Capacity)
            {
                SetCapacity(sizeOf, alignOf, length);
            }

            Length = length;

            if (options == NativeArrayOptions.ClearMemory
                && oldLength < length)
            {
                var num = length - oldLength;
                byte* ptr = (byte*)Ptr;
                UnsafeUtility.MemClear(ptr + oldLength * sizeOf, num * sizeOf);
            }
        }

        /// <summary>
        /// Changes the list length, resizing if necessary.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="length">The new length of the list.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        public void Resize<T>(int length, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) where T : struct
        {
            Resize(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), length, options);
        }

        void Realloc<U>(ref U allocator, int sizeOf, int alignOf, int capacity) where U : unmanaged, AllocatorManager.IAllocator
        {
            void* newPointer = null;

            if (capacity > 0)
            {
                newPointer = allocator.Allocate(sizeOf, alignOf, capacity);

                if (Capacity > 0)
                {
                    var itemsToCopy = math.min(capacity, Capacity);
                    var bytesToCopy = itemsToCopy * sizeOf;
                    UnsafeUtility.MemCpy(newPointer, Ptr, bytesToCopy);
                }
            }

            allocator.Free(Ptr, sizeOf, alignOf, Capacity);

            Ptr = newPointer;
            Capacity = capacity;
            Length = math.min(Length, capacity);
        }

        void Realloc(int sizeOf, int alignOf, int capacity)
        {
            Realloc(ref Allocator, sizeOf, alignOf, capacity);
        }

        void SetCapacity<U>(ref U allocator, int sizeOf, int alignOf, int capacity) where U : unmanaged, AllocatorManager.IAllocator
        {
            var newCapacity = math.max(capacity, 64 / sizeOf);
            newCapacity = math.ceilpow2(newCapacity);

            if (newCapacity == Capacity)
            {
                return;
            }

            Realloc(ref allocator, sizeOf, alignOf, newCapacity);
        }

        void SetCapacity(int sizeOf, int alignOf, int capacity)
        {
            SetCapacity(ref Allocator, sizeOf, alignOf, capacity);
        }

        /// <summary>
        /// Set the number of items that can fit in the container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="capacity">The number of items that the container can hold before it resizes its internal storage.</param>
        public void SetCapacity<T>(int capacity) where T : struct
        {
            SetCapacity(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), capacity);
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        public void TrimExcess<T>() where T : struct
        {
            if (Capacity != Length)
            {
                Realloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), Length);
            }
        }

        /// <summary>
        /// Searches for the specified element in list.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="value"></param>
        /// <returns>The zero-based index of the first occurrence element if found, otherwise returns -1.</returns>
        public int IndexOf<T>(T value) where T : struct, IEquatable<T>
        {
            return NativeArrayExtensions.IndexOf<T, T>(Ptr, Length, value);
        }

        /// <summary>
        /// Determines whether an element is in the list.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="value"></param>
        /// <returns>True, if element is found.</returns>
        public bool Contains<T>(T value) where T : struct, IEquatable<T>
        {
            return IndexOf(value) != -1;
        }

        /// <summary>
        /// Adds an element to the list.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="value">The value to be added at the end of the list.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void AddNoResize<T>(T value) where T : struct
        {
            CheckNoResizeHasEnoughCapacity(1);
            UnsafeUtility.WriteArrayElement(Ptr, Length, value);
            Length += 1;
        }

        void AddRangeNoResize(int sizeOf, void* ptr, int length)
        {
            CheckNoResizeHasEnoughCapacity(length);
            void* dst = (byte*)Ptr + Length * sizeOf;
            UnsafeUtility.MemCpy(dst, ptr, length * sizeOf);
            Length += length;
        }

        /// <summary>
        /// Adds elements from a buffer to this list.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="ptr">A pointer to the buffer.</param>
        /// <param name="length">The number of elements to add to the list.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void AddRangeNoResize<T>(void* ptr, int length) where T : struct
        {
            AddRangeNoResize(UnsafeUtility.SizeOf<T>(), ptr, length);
        }

        /// <summary>
        /// Adds elements from a list to this list.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="list">Other container to copy elements from.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void AddRangeNoResize<T>(UnsafeList list) where T : struct
        {
            AddRangeNoResize(UnsafeUtility.SizeOf<T>(), list.Ptr, CollectionHelper.AssumePositive(list.Length));
        }

        /// <summary>
        /// Adds an element to the list.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="value">The value to be added at the end of the list.</param>
        /// <remarks>
        /// If the list has reached its current capacity, it copies the original, internal array to
        /// a new, larger array, and then deallocates the original.
        /// </remarks>
        public void Add<T>(T value) where T : struct
        {
            var idx = Length;

            if (Length + 1 > Capacity)
            {
                Resize<T>(idx + 1);
            }
            else
            {
                Length += 1;
            }

            UnsafeUtility.WriteArrayElement(Ptr, idx, value);
        }

        void AddRange(int sizeOf, int alignOf, void* ptr, int length)
        {
            var idx = Length;

            if (Length + length > Capacity)
            {
                Resize(sizeOf, alignOf, Length + length);
            }
            else
            {
                Length += length;
            }

            void* dst = (byte*)Ptr + idx * sizeOf;
            UnsafeUtility.MemCpy(dst, ptr, length * sizeOf);
        }

        /// <summary>
        /// Adds elements from a buffer to this list.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="ptr">A pointer to the buffer.</param>
        /// <param name="length">The number of elements to add to the list.</param>
        public void AddRange<T>(void* ptr, int length) where T : struct
        {
            AddRange(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), ptr, length);
        }

        /// <summary>
        /// Adds elements from a list to this list.
        /// </summary>
        /// <remarks>
        /// If the list has reached its current capacity, it copies the original, internal array to
        /// a new, larger array, and then deallocates the original.
        /// </remarks>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="list">Other container to copy elements from.</param>
        public void AddRange<T>(UnsafeList list) where T : struct
        {
            AddRange(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), list.Ptr, list.Length);
        }

        void InsertRangeWithBeginEnd(int sizeOf, int alignOf, int begin, int end)
        {
            CheckBeginEnd(begin, end);

            int items = end - begin;
            if (items < 1)
            {
                return;
            }

            var oldLength = Length;

            if (Length + items > Capacity)
            {
                Resize(sizeOf, alignOf, Length + items);
            }
            else
            {
                Length += items;
            }

            var itemsToCopy = oldLength - begin;

            if (itemsToCopy < 1)
            {
                return;
            }

            var bytesToCopy = itemsToCopy * sizeOf;
            unsafe
            {
                byte* ptr = (byte*)Ptr;
                byte* dest = ptr + end * sizeOf;
                byte* src = ptr + begin * sizeOf;
                UnsafeUtility.MemMove(dest, src, bytesToCopy);
            }
        }

        /// <summary>
        /// Inserts a number of items into a container at a specified zero-based index.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="begin">The zero-based index at which the new elements should be inserted.</param>
        /// <param name="end">The zero-based index just after where the elements should be removed.</param>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        public void InsertRangeWithBeginEnd<T>(int begin, int end) where T : struct
        {
            InsertRangeWithBeginEnd(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), begin, end);
        }

        void RemoveRangeSwapBackWithBeginEnd(int sizeOf, int begin, int end)
        {
            CheckBeginEnd(begin, end);

            int itemsToRemove = end - begin;
            if (itemsToRemove > 0)
            {
                int copyFrom = math.max(Length - itemsToRemove, end);
                void* dst = (byte*)Ptr + begin * sizeOf;
                void* src = (byte*)Ptr + copyFrom * sizeOf;
                UnsafeUtility.MemCpy(dst, src, (Length - copyFrom) * sizeOf);
                Length -= itemsToRemove;
            }
        }

        /// <summary>
        /// Truncates the list by replacing the item at the specified index with the last item in the list. The list
        /// is shortened by one.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="index">The index of the item to delete.</param>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        public void RemoveAtSwapBack<T>(int index) where T : struct
        {
            RemoveRangeSwapBackWithBeginEnd<T>(index, index + 1);
        }

        /// <summary>
        /// Truncates the list by replacing the item at the specified index range with the items from the end the list. The list
        /// is shortened by number of elements in range.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="begin">The first index of the item to remove.</param>
        /// <param name="end">The index past-the-last item to remove.</param>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        public void RemoveRangeSwapBackWithBeginEnd<T>(int begin, int end) where T : struct
        {
            RemoveRangeSwapBackWithBeginEnd(UnsafeUtility.SizeOf<T>(), begin, end);
        }

        void RemoveRangeWithBeginEnd(int sizeOf, int begin, int end)
        {
            CheckBeginEnd(begin, end);

            int itemsToRemove = end - begin;
            if (itemsToRemove > 0)
            {
                int copyFrom = math.min(begin + itemsToRemove, Length);
                void* dst = (byte*)Ptr + begin * sizeOf;
                void* src = (byte*)Ptr + copyFrom * sizeOf;
                UnsafeUtility.MemCpy(dst, src, (Length - copyFrom) * sizeOf);
                Length -= itemsToRemove;
            }
        }

        /// <summary>
        /// Truncates the list by removing the item at the specified index, and shifting all remaining items to replace removed item. The list
        /// is shortened by one.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="index">The index of the item to delete.</param>
        /// <remarks>
        /// This method of removing item is useful only in case when list is ordered and user wants to preserve order
        /// in list after removal In majority of cases is not important and user should use more performant `RemoveAtSwapBack`.
        /// </remarks>
        public void RemoveAt<T>(int index) where T : struct
        {
            RemoveRangeWithBeginEnd<T>(index, index + 1);
        }

        /// <summary>
        /// Truncates the list by removing the items at the specified index range, and shifting all remaining items to replace removed items. The list
        /// is shortened by number of elements in range.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="begin">The first index of the item to remove.</param>
        /// <param name="end">The index past-the-last item to remove.</param>
        /// <remarks>
        /// This method of removing item(s) is useful only in case when list is ordered and user wants to preserve order
        /// in list after removal In majority of cases is not important and user should use more performant `RemoveRangeSwapBackWithBeginEnd`.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        public void RemoveRangeWithBeginEnd<T>(int begin, int end) where T : struct
        {
            RemoveRangeWithBeginEnd(UnsafeUtility.SizeOf<T>(), begin, end);
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
            public readonly void* Ptr;

            /// <summary>
            ///
            /// </summary>
            public readonly int Length;

            internal ParallelReader(void* ptr, int length)
            {
                Ptr = ptr;
                Length = length;
            }

            /// <summary>
            ///
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="value"></param>
            /// <returns></returns>
            public int IndexOf<T>(T value) where T : struct, IEquatable<T>
            {
                return NativeArrayExtensions.IndexOf<T, T>(Ptr, Length, value);
            }

            /// <summary>
            ///
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="value"></param>
            /// <returns></returns>
            public bool Contains<T>(T value) where T : struct, IEquatable<T>
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
            /// <typeparam name="T">Source type of elements</typeparam>
            /// <param name="value">The value to be added at the end of the list.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            public void AddNoResize<T>(T value) where T : struct
            {
                var idx = Interlocked.Increment(ref ListData->Length) - 1;
                ListData->CheckNoResizeHasEnoughCapacity(idx, 1);
                UnsafeUtility.WriteArrayElement(Ptr, idx, value);
            }

            void AddRangeNoResize(int sizeOf, int alignOf, void* ptr, int length)
            {
                var idx = Interlocked.Add(ref ListData->Length, length) - length;
                ListData->CheckNoResizeHasEnoughCapacity(idx, length);
                void* dst = (byte*)Ptr + idx * sizeOf;
                UnsafeUtility.MemCpy(dst, ptr, length * sizeOf);
            }

            /// <summary>
            /// Adds elements from a buffer to this list.
            /// </summary>
            /// <typeparam name="T">Source type of elements</typeparam>
            /// <param name="ptr">A pointer to the buffer.</param>
            /// <param name="length">The number of elements to add to the list.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            public void AddRangeNoResize<T>(void* ptr, int length) where T : struct
            {
                AddRangeNoResize(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), ptr, length);
            }

            /// <summary>
            /// Adds elements from a list to this list.
            /// </summary>
            /// <typeparam name="T">Source type of elements</typeparam>
            /// <param name="list">Other container to copy elements from.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            public void AddRangeNoResize<T>(UnsafeList list) where T : struct
            {
                AddRangeNoResize(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), list.Ptr, list.Length);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void CheckNull(void* listData)
        {
            if (listData == null)
            {
                throw new Exception("UnsafeList has yet to be created or has been destroyed!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckAllocator(Allocator a)
        {
            if (!CollectionHelper.ShouldDeallocate(a))
            {
                throw new Exception("UnsafeList is not initialized, it must be initialized with allocator before use.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckAllocator(AllocatorManager.AllocatorHandle a)
        {
            if (!CollectionHelper.ShouldDeallocate(a))
            {
                throw new Exception("UnsafeList is not initialized, it must be initialized with allocator before use.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckBeginEnd(int begin, int end)
        {
            if (begin > end)
            {
                throw new ArgumentException($"Value for begin {begin} index must less or equal to end {end}.");
            }

            if (begin < 0)
            {
                throw new ArgumentOutOfRangeException($"Value for begin {begin} must be positive.");
            }

            if (begin > Length)
            {
                throw new ArgumentOutOfRangeException($"Value for begin {begin} is out of bounds.");
            }

            if (end > Length)
            {
                throw new ArgumentOutOfRangeException($"Value for end {end} is out of bounds.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckNoResizeHasEnoughCapacity(int length)
        {
            CheckNoResizeHasEnoughCapacity(length, Length);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckNoResizeHasEnoughCapacity(int length, int index)
        {
            if (Capacity < index + length)
            {
                throw new Exception($"AddNoResize assumes that list capacity is sufficient (Capacity {Capacity}, Length {Length}), requested length {length}!");
            }
        }
    }

    /// <summary>
    /// Provides extension methods for UnsafeList.
    /// </summary>
    public static class UnsafeListExtension
    {
        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        internal static ref UnsafeList ListData<T>(ref this UnsafeList<T> from) where T : unmanaged => ref UnsafeUtility.As<UnsafeList<T>, UnsafeList>(ref from);

        /// <summary>
        /// Sorts a list in ascending order.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="list">List to perform sort.</param>
        public unsafe static void Sort<T>(this UnsafeList list) where T : unmanaged, IComparable<T>
        {
            list.Sort<T, NativeSortExtension.DefaultComparer<T>>(new NativeSortExtension.DefaultComparer<T>());
        }

        /// <summary>
        /// Sorts a list using a custom comparison function.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <typeparam name="U">The comparer type.</typeparam>
        /// <param name="list">List to perform sort.</param>
        /// <param name="comp">A comparison function that indicates whether one element in the array is less than, equal to, or greater than another element.</param>
        public unsafe static void Sort<T, U>(this UnsafeList list, U comp) where T : unmanaged where U : IComparer<T>
        {
            NativeSortExtension.IntroSort<T, U>(list.Ptr, list.Length, comp);
        }

        /// <summary>
        /// Sorts the container in ascending order.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">The container to perform sort.</param>
        /// <param name="inputDeps">The job handle or handles for any scheduled jobs that use this container.</param>
        /// <returns>A new job handle containing the prior handles as well as the handle for the job that sorts
        /// the container.</returns>
        [NotBurstCompatible /* This is not burst compatible because of IJob's use of a static IntPtr. Should switch to IJobBurstSchedulable in the future */]
        [Obsolete("Instead call SortJob(this UnsafeList).Schedule(JobHandle). (RemovedAfter 2021-06-20)", false)]
        public unsafe static JobHandle Sort<T>(this UnsafeList container, JobHandle inputDeps)
            where T : unmanaged, IComparable<T>
        {
            return container.Sort<T, NativeSortExtension.DefaultComparer<T>>(new NativeSortExtension.DefaultComparer<T>(), inputDeps);
        }

        /// <summary>
        /// Creates a job that will sort a list in ascending order.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="list">List to sort.</param>
        /// <returns>The job that will sort the list. Scheduling the job is left to the user.</returns>
        public unsafe static SortJob<T, NativeSortExtension.DefaultComparer<T>> SortJob<T>(this UnsafeList list)
            where T : unmanaged, IComparable<T>
        {
            return NativeSortExtension.SortJob((T*)list.Ptr, list.Length, new NativeSortExtension.DefaultComparer<T>());
        }

        /// <summary>
        /// Sorts the container using a custom comparison function.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <typeparam name="U">The comparer type.</typeparam>
        /// <param name="container">The container to perform sort.</param>
        /// <param name="comp">A comparison function that indicates whether one element in the array is less than, equal to, or greater than another element.</param>
        /// <param name="inputDeps">The job handle or handles for any scheduled jobs that use this container.</param>
        /// <returns>A new job handle containing the prior handles as well as the handle for the job that sorts
        /// the container.</returns>
        [NotBurstCompatible /* This is not burst compatible because of IJob's use of a static IntPtr. Should switch to IJobBurstSchedulable in the future */]
        [Obsolete("Instead call SortJob(this UnsafeList, U).Schedule(JobHandle). (RemovedAfter 2021-06-20)", false)]
        public unsafe static JobHandle Sort<T, U>(this UnsafeList container, U comp, JobHandle inputDeps)
            where T : unmanaged
            where U : IComparer<T>
        {
            return NativeSortExtension.Sort((T*)container.Ptr, container.Length, comp, inputDeps);
        }

        /// <summary>
        /// Creates a job that will sort a list using a comparison function.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <typeparam name="U">The comparer type.</typeparam>
        /// <param name="list">List to sort.</param>
        /// <param name="comp">A comparison function that indicates whether one element in the array is less than, equal to, or greater than another element.</param>
        /// <returns>The job that will sort the list. Scheduling the job is left to the user.</returns>
        public unsafe static SortJob<T, U> SortJob<T, U>(this UnsafeList list, U comp)
            where T : unmanaged
            where U : IComparer<T>
        {
            return NativeSortExtension.SortJob((T*)list.Ptr, list.Length, comp);
        }

        /// <summary>
        /// Binary search for the value in the sorted container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">The container to perform search.</param>
        /// <param name="value">The value to search for.</param>
        /// <returns>Positive index of the specified value if value is found. Otherwise bitwise complement of index of first greater value.</returns>
        /// <remarks>Array must be sorted, otherwise value searched might not be found even when it is in array. IComparer corresponds to IComparer used by sort.</remarks>
        public static int BinarySearch<T>(this UnsafeList container, T value)
            where T : unmanaged, IComparable<T>
        {
            return container.BinarySearch(value, new NativeSortExtension.DefaultComparer<T>());
        }

        /// <summary>
        /// Binary search for the value in the sorted container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <typeparam name="U">The comparer type.</typeparam>
        /// <param name="container">The container to perform search.</param>
        /// <param name="value">The value to search for.</param>
        /// <param name="comp">A comparison function that indicates whether one element in the array is less than, equal to, or greater than another element.</param>
        /// <returns>Positive index of the specified value if value is found. Otherwise bitwise complement of index of first greater value.</returns>
        /// <remarks>Array must be sorted, otherwise value searched might not be found even when it is in array. IComparer corresponds to IComparer used by sort.</remarks>
        public unsafe static int BinarySearch<T, U>(this UnsafeList container, T value, U comp)
            where T : unmanaged
            where U : IComparer<T>
        {
            return NativeSortExtension.BinarySearch((T*)container.Ptr, container.Length, value, comp);
        }

    }


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

        public readonly int unused;

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
        public unsafe UnsafePtrList(void** ptr, int length) : this()
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
        public unsafe UnsafePtrList(int initialCapacity, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) : this()
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
        public unsafe UnsafePtrList(int initialCapacity, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) : this()
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
        /// <value>True if this string has no characters or if the container has not been constructed.</value>
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

        public FixedString128Bytes[] Table
        {
            get
            {
                var table = new FixedString128Bytes[m_wordStorage.Entries];
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
        NativeParallelMultiHashMap<int, int> hash; // from string hash to table entry
        int chars; // bytes in buffer allocated so far
        int entries; // number of strings allocated so far

        /// <summary>
        /// For internal use only.
        /// </summary>
        [NotBurstCompatible /* Deprecated */]
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

        /// <summary>
        ///
        /// </summary>
        [NotBurstCompatible /* Deprecated */]
        public static void Initialize()
        {
            if (WordStorageStatic.Ref.Data.buffer.IsCreated)
                return;
            WordStorageStatic.Ref.Data.buffer = new NativeArray<byte>(kMaxChars, Allocator.Persistent);
            WordStorageStatic.Ref.Data.entry = new NativeArray<Entry>(kMaxEntries, Allocator.Persistent);
            WordStorageStatic.Ref.Data.hash = new NativeParallelMultiHashMap<int, int>(kMaxEntries, Allocator.Persistent);
            Clear();
#if !UNITY_DOTSRUNTIME
            // Free storage on domain unload, which happens when iterating on the Entities module a lot.
            AppDomain.CurrentDomain.DomainUnload += (_, __) => { Shutdown(); };

            // There is no domain unload in player builds, so we must be sure to shutdown when the process exits.
            AppDomain.CurrentDomain.ProcessExit += (_, __) => { Shutdown(); };
#endif
        }

        /// <summary>
        ///
        /// </summary>
        [NotBurstCompatible /* Deprecated */]
        public static void Shutdown()
        {
            if (!WordStorageStatic.Ref.Data.buffer.IsCreated)
                return;
            WordStorageStatic.Ref.Data.buffer.Dispose();
            WordStorageStatic.Ref.Data.entry.Dispose();
            WordStorageStatic.Ref.Data.hash.Dispose();
            WordStorageStatic.Ref.Data = default;
        }

        /// <summary>
        ///
        /// </summary>
        [NotBurstCompatible /* Deprecated */]
        public static void Clear()
        {
            Initialize();
            WordStorageStatic.Ref.Data.chars = 0;
            WordStorageStatic.Ref.Data.entries = 0;
            WordStorageStatic.Ref.Data.hash.Clear();
            var temp = new FixedString32Bytes();
            WordStorageStatic.Ref.Data.GetOrCreateIndex(ref temp); // make sure that Index=0 means empty string
        }

        /// <summary>
        ///
        /// </summary>
        [NotBurstCompatible /* Deprecated */]
        public static void Setup()
        {
            Clear();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <param name="temp"></param>
        /// <typeparam name="T"></typeparam>
        public unsafe void GetFixedString<T>(int index, ref T temp)
        where T : IUTF8Bytes, INativeList<byte>
        {
            Assert.IsTrue(index < entries);
            var e = entry[index];
            Assert.IsTrue(e.length <= kMaxCharsPerEntry);
            temp.Length = e.length;
            UnsafeUtility.MemCpy(temp.GetUnsafePtr(), (byte*)buffer.GetUnsafePtr() + e.offset, temp.Length);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="h"></param>
        /// <param name="temp"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public int GetIndexFromHashAndFixedString<T>(int h, ref T temp)
        where T : IUTF8Bytes, INativeList<byte>
        {
            Assert.IsTrue(temp.Length <= kMaxCharsPerEntry); // about one printed page of text
            int itemIndex;
            NativeParallelMultiHashMapIterator<int> iter;
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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
        [NotBurstCompatible /* Deprecated */]
        public unsafe bool Contains(string value)
        {
            FixedString512Bytes temp = value;
            return Contains(ref temp);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
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
            FixedString512Bytes temp = default;
            ToFixedString(ref temp);
            return temp.ToString();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
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
            FixedString512Bytes temp = value;
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

        [NotBurstCompatible /* Deprecated */]
        string NewString(char c, int count)
        {
            char[] temp = new char[count];
            for (var i = 0; i < count; ++i)
                temp[i] = c;
            return new string(temp, 0, count);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="result"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [NotBurstCompatible /* Deprecated */]
        public int ToFixedString<T>(ref T result)
        where T : IUTF8Bytes, INativeList<byte>
        {
            unsafe
            {
                var positiveNumericSuffix = PositiveNumericSuffix;
                var leadingZeroes = LeadingZeroes;

                WordStorage.Instance.GetFixedString(Index, ref result);
                if (positiveNumericSuffix == 0 && leadingZeroes == 0)
                    return 0;

                // print the numeric suffix, if any, backwards, as ASCII, to a little buffer.
                const int maximumDigits = kMaxLeadingZeroes + 10;
                var buffer = stackalloc byte[maximumDigits];
                var firstDigit = maximumDigits;
                while (positiveNumericSuffix > 0)
                {
                    buffer[--firstDigit] = (byte)('0' + positiveNumericSuffix % 10);
                    positiveNumericSuffix /= 10;
                }
                while (leadingZeroes-- > 0)
                    buffer[--firstDigit] = (byte)'0';

                // make space in the output for leading zeroes if any, followed by the positive numeric index if any.
                var dest = result.GetUnsafePtr() + result.Length;
                result.Length += maximumDigits - firstDigit;
                while (firstDigit < maximumDigits)
                    *dest++ = buffer[firstDigit++];
                return 0;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        [NotBurstCompatible /* Deprecated */]
        public override string ToString()
        {
            FixedString512Bytes temp = default;
            ToFixedString(ref temp);
            return temp.ToString();
        }

        bool IsDigit(byte b)
        {
            return b >= '0' && b <= '9';
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        [NotBurstCompatible /* Deprecated */]
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
        [NotBurstCompatible /* Deprecated */]
        public void SetString(string value)
        {
            FixedString512Bytes temp = value;
            SetString(ref temp);
        }
    }

    [Obsolete("UntypedUnsafeHashMap is renamed to UntypedUnsafeParallelHashMap. (UnityUpgradable) -> UntypedUnsafeParallelHashMap", false)]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UntypedUnsafeHashMap
    {
#pragma warning disable 169
        [NativeDisableUnsafePtrRestriction]
        UnsafeParallelHashMapData* m_Buffer;
        AllocatorManager.AllocatorHandle m_AllocatorLabel;
#pragma warning restore 169
    }

    [Obsolete("UnsafeHashMap is renamed to UnsafeParallelHashMap. (UnityUpgradable) -> UnsafeParallelHashMap<TKey, TValue>", false)]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafeHashMap<TKey, TValue>
        : INativeDisposable
        , IEnumerable<KeyValue<TKey, TValue>> // Used by collection initializers.
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeParallelHashMapData* m_Buffer;
        internal AllocatorManager.AllocatorHandle m_AllocatorLabel;

        /// <summary>
        /// Initializes and returns an instance of UnsafeHashMap.
        /// </summary>
        /// <param name="capacity">The number of key-value pairs that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
        public UnsafeHashMap(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            CollectionHelper.CheckIsUnmanaged<TKey>();
            CollectionHelper.CheckIsUnmanaged<TValue>();

            m_AllocatorLabel = allocator;
            // Bucket size if bigger to reduce collisions
            UnsafeParallelHashMapData.AllocateHashMap<TKey, TValue>(capacity, capacity * 2, allocator, out m_Buffer);

            Clear();
        }

        /// <summary>
        /// Whether this hash map is empty.
        /// </summary>
        /// <value>True if this hash map is empty or the hash map has not been constructed.</value>
        public bool IsEmpty => !IsCreated || UnsafeParallelHashMapData.IsEmpty(m_Buffer);

        /// <summary>
        /// The current number of key-value pairs in this hash map.
        /// </summary>
        /// <returns>The current number of key-value pairs in this hash map.</returns>
        public int Count() => UnsafeParallelHashMapData.GetCount(m_Buffer);

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
                UnsafeParallelHashMapData* data = m_Buffer;
                return data->keyCapacity;
            }

            set
            {
                UnsafeParallelHashMapData* data = m_Buffer;
                UnsafeParallelHashMapData.ReallocateHashMap<TKey, TValue>(data, value, UnsafeParallelHashMapData.GetBucketSize(value), m_AllocatorLabel);
            }
        }

        /// <summary>
        /// Removes all key-value pairs.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            UnsafeParallelHashMapBase<TKey, TValue>.Clear(m_Buffer);
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
            return UnsafeParallelHashMapBase<TKey, TValue>.TryAdd(m_Buffer, key, item, false, m_AllocatorLabel);
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
            TryAdd(key, item);
        }

        /// <summary>
        /// Removes a key-value pair.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>True if a key-value pair was removed.</returns>
        public bool Remove(TKey key)
        {
            return UnsafeParallelHashMapBase<TKey, TValue>.Remove(m_Buffer, key, false) != 0;
        }

        /// <summary>
        /// Returns the value associated with a key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="item">Outputs the value associated with the key. Outputs default if the key was not present.</param>
        /// <returns>True if the key was present.</returns>
        public bool TryGetValue(TKey key, out TValue item)
        {
            NativeParallelMultiHashMapIterator<TKey> tempIt;
            return UnsafeParallelHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(m_Buffer, key, out item, out tempIt);
        }

        /// <summary>
        /// Returns true if a given key is present in this hash map.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>True if the key was present.</returns>
        public bool ContainsKey(TKey key)
        {
            return UnsafeParallelHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(m_Buffer, key, out var tempValue, out var tempIt);
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
                TValue res;
                TryGetValue(key, out res);
                return res;
            }

            set
            {
                if (UnsafeParallelHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(m_Buffer, key, out var item, out var iterator))
                {
                    UnsafeParallelHashMapBase<TKey, TValue>.SetValue(m_Buffer, ref iterator, ref value);
                }
                else
                {
                    UnsafeParallelHashMapBase<TKey, TValue>.TryAdd(m_Buffer, key, value, false, m_AllocatorLabel);
                }
            }
        }

        /// <summary>
        /// Whether this hash map has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this hash map has been allocated (and not yet deallocated).</value>
        public bool IsCreated => m_Buffer != null;

        /// <summary>
        /// Releases all resources (memory).
        /// </summary>
        public void Dispose()
        {
            UnsafeParallelHashMapData.DeallocateHashMap(m_Buffer, m_AllocatorLabel);
            m_Buffer = null;
        }

        /// <summary>
        /// Creates and schedules a job that will dispose this hash map.
        /// </summary>
        /// <param name="inputDeps">A job handle. The newly scheduled job will depend upon this handle.</param>
        /// <returns>The handle of a new job that will dispose this hash map.</returns>
        [NotBurstCompatible /* This is not burst compatible because of IJob's use of a static IntPtr. Should switch to IJobBurstSchedulable in the future */]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new UnsafeParallelHashMapDisposeJob { Data = m_Buffer, Allocator = m_AllocatorLabel }.Schedule(inputDeps);
            m_Buffer = null;
            return jobHandle;
        }

        /// <summary>
        /// Returns an array with a copy of all this hash map's keys (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all this hash map's keys (in no particular order).</returns>
        public NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            var result = CollectionHelper.CreateNativeArray<TKey>(UnsafeParallelHashMapData.GetCount(m_Buffer), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeParallelHashMapData.GetKeyArray(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// Returns an array with a copy of all this hash map's values (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all this hash map's values (in no particular order).</returns>
        public NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            var result = CollectionHelper.CreateNativeArray<TValue>(UnsafeParallelHashMapData.GetCount(m_Buffer), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeParallelHashMapData.GetValueArray(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// Returns a NativeKeyValueArrays with a copy of all this hash map's keys and values.
        /// </summary>
        /// <remarks>The key-value pairs are copied in no particular order. For all `i`, `Values[i]` will be the value associated with `Keys[i]`.</remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>A NativeKeyValueArrays with a copy of all this hash map's keys and values.</returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            var result = new NativeKeyValueArrays<TKey, TValue>(UnsafeParallelHashMapData.GetCount(m_Buffer), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeParallelHashMapData.GetKeyValueArrays(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// Returns a parallel writer for this hash map.
        /// </summary>
        /// <returns>A parallel writer for this hash map.</returns>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;
            writer.m_ThreadIndex = 0;
            writer.m_Buffer = m_Buffer;
            return writer;
        }

        /// <summary>
        /// A parallel writer for a UnsafeParallelHashMap.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a UnsafeParallelHashMap.
        /// </remarks>
        [NativeContainerIsAtomicWriteOnly]
        public unsafe struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeParallelHashMapData* m_Buffer;

            [NativeSetThreadIndex]
            internal int m_ThreadIndex;

            /// <summary>
            /// The number of key-value pairs that fit in the current allocation.
            /// </summary>
            /// <value>The number of key-value pairs that fit in the current allocation.</value>
            public int Capacity
            {
                get
                {
                    UnsafeParallelHashMapData* data = m_Buffer;
                    return data->keyCapacity;
                }
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
                Assert.IsTrue(m_ThreadIndex >= 0);
                return UnsafeParallelHashMapBase<TKey, TValue>.TryAddAtomic(m_Buffer, key, item, m_ThreadIndex);
            }
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator { m_Enumerator = new UnsafeParallelHashMapDataEnumerator(m_Buffer) };
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<KeyValue<TKey, TValue>> IEnumerable<KeyValue<TKey, TValue>>.GetEnumerator()
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
        /// An enumerator over the key-value pairs of a hash map.
        /// </summary>
        /// <remarks>
        /// In an enumerator's initial state, <see cref="Current"/> is not valid to read.
        /// From this state, the first <see cref="MoveNext"/> call advances the enumerator to the first key-value pair.
        /// </remarks>
        public struct Enumerator : IEnumerator<KeyValue<TKey, TValue>>
        {
            internal UnsafeParallelHashMapDataEnumerator m_Enumerator;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next key-value pair.
            /// </summary>
            /// <returns>True if <see cref="Current"/> is valid to read after the call.</returns>
            public bool MoveNext() => m_Enumerator.MoveNext();

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset() => m_Enumerator.Reset();

            /// <summary>
            /// The current key-value pair.
            /// </summary>
            /// <value>The current key-value pair.</value>
            public KeyValue<TKey, TValue> Current => m_Enumerator.GetCurrent<TKey, TValue>();

            object IEnumerator.Current => Current;
        }
    }

    [Obsolete("UnsafeMultiHashMap is renamed to UnsafeParallelMultiHashMap. (UnityUpgradable) -> UnsafeParallelMultiHashMap<TKey, TValue>", false)]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafeMultiHashMap<TKey, TValue>
        : INativeDisposable
        , IEnumerable<KeyValue<TKey, TValue>> // Used by collection initializers.
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeParallelHashMapData* m_Buffer;
        internal AllocatorManager.AllocatorHandle m_AllocatorLabel;

        /// <summary>
        /// Initializes and returns an instance of UnsafeParallelMultiHashMap.
        /// </summary>
        /// <param name="capacity">The number of key-value pairs that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
        public UnsafeMultiHashMap(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_AllocatorLabel = allocator;
            // Bucket size if bigger to reduce collisions
            UnsafeParallelHashMapData.AllocateHashMap<TKey, TValue>(capacity, capacity * 2, allocator, out m_Buffer);
            Clear();
        }

        /// <summary>
        /// Whether this hash map is empty.
        /// </summary>
        /// <value>True if this hash map is empty or the hash map has not been constructed.</value>
        public bool IsEmpty => !IsCreated || UnsafeParallelHashMapData.IsEmpty(m_Buffer);

        /// <summary>
        /// Returns the current number of key-value pairs in this hash map.
        /// </summary>
        /// <remarks>Key-value pairs with matching keys are counted as separate, individual pairs.</remarks>
        /// <returns>The current number of key-value pairs in this hash map.</returns>
        public int Count()
        {
            if (m_Buffer->allocatedIndexLength <= 0)
            {
                return 0;
            }

            return UnsafeParallelHashMapData.GetCount(m_Buffer);
        }

        /// <summary>
        /// Returns the number of key-value pairs that fit in the current allocation.
        /// </summary>
        /// <value>The number of key-value pairs that fit in the current allocation.</value>
        /// <param name="value">A new capacity. Must be larger than the current capacity.</param>
        /// <exception cref="Exception">Thrown if `value` is less than the current capacity.</exception>
        public int Capacity
        {
            get
            {
                UnsafeParallelHashMapData* data = m_Buffer;
                return data->keyCapacity;
            }

            set
            {
                UnsafeParallelHashMapData* data = m_Buffer;
                UnsafeParallelHashMapData.ReallocateHashMap<TKey, TValue>(data, value, UnsafeParallelHashMapData.GetBucketSize(value), m_AllocatorLabel);
            }
        }

        /// <summary>
        /// Removes all key-value pairs.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            UnsafeParallelHashMapBase<TKey, TValue>.Clear(m_Buffer);
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <remarks>
        /// If a key-value pair with this key is already present, an additional separate key-value pair is added.
        /// </remarks>
        /// <param name="key">The key to add.</param>
        /// <param name="item">The value to add.</param>
        public void Add(TKey key, TValue item)
        {
            UnsafeParallelHashMapBase<TKey, TValue>.TryAdd(m_Buffer, key, item, true, m_AllocatorLabel);
        }

        /// <summary>
        /// Removes a key and its associated value(s).
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>The number of removed key-value pairs. If the key was not present, returns 0.</returns>
        public int Remove(TKey key)
        {
            return UnsafeParallelHashMapBase<TKey, TValue>.Remove(m_Buffer, key, true);
        }

        /// <summary>
        /// Removes all key-value pairs with a particular key and a particular value.
        /// </summary>
        /// <remarks>Removes all key-value pairs which have a particular key and which *also have* a particular value.
        /// In other words: (key *AND* value) rather than (key *OR* value).</remarks>
        /// <typeparam name="TValueEQ">The type of the value.</typeparam>
        /// <param name="key">The key of the key-value pairs to remove.</param>
        /// <param name="value">The value of the key-value pairs to remove.</param>
        public void Remove<TValueEQ>(TKey key, TValueEQ value)
            where TValueEQ : struct, IEquatable<TValueEQ>
        {
            UnsafeParallelHashMapBase<TKey, TValueEQ>.RemoveKeyValue(m_Buffer, key, value);
        }

        /// <summary>
        /// Removes a single key-value pair.
        /// </summary>
        /// <param name="it">An iterator representing the key-value pair to remove.</param>
        /// <exception cref="InvalidOperationException">Thrown if the iterator is invalid.</exception>
        public void Remove(NativeParallelMultiHashMapIterator<TKey> it)
        {
            UnsafeParallelHashMapBase<TKey, TValue>.Remove(m_Buffer, it);
        }

        /// <summary>
        /// Gets an iterator for a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="item">Outputs the associated value represented by the iterator.</param>
        /// <param name="it">Outputs an iterator.</param>
        /// <returns>True if the key was present.</returns>
        public bool TryGetFirstValue(TKey key, out TValue item, out NativeParallelMultiHashMapIterator<TKey> it)
        {
            return UnsafeParallelHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(m_Buffer, key, out item, out it);
        }

        /// <summary>
        /// Advances an iterator to the next value associated with its key.
        /// </summary>
        /// <param name="item">Outputs the next value.</param>
        /// <param name="it">A reference to the iterator to advance.</param>
        /// <returns>True if the key was present and had another value.</returns>
        public bool TryGetNextValue(out TValue item, ref NativeParallelMultiHashMapIterator<TKey> it)
        {
            return UnsafeParallelHashMapBase<TKey, TValue>.TryGetNextValueAtomic(m_Buffer, out item, ref it);
        }

        /// <summary>
        /// Returns true if a given key is present in this hash map.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>True if the key was present in this hash map.</returns>
        public bool ContainsKey(TKey key)
        {
            return TryGetFirstValue(key, out var temp0, out var temp1);
        }

        /// <summary>
        /// Returns the number of values associated with a given key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>The number of values associated with the key. Returns 0 if the key was not present.</returns>
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
        /// Sets a new value for an existing key-value pair.
        /// </summary>
        /// <param name="item">The new value.</param>
        /// <param name="it">The iterator representing a key-value pair.</param>
        /// <returns>True if a value was overwritten.</returns>
        public bool SetValue(TValue item, NativeParallelMultiHashMapIterator<TKey> it)
        {
            return UnsafeParallelHashMapBase<TKey, TValue>.SetValue(m_Buffer, ref it, ref item);
        }

        /// <summary>
        /// Whether this hash map has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this hash map has been allocated (and not yet deallocated).</value>
        public bool IsCreated => m_Buffer != null;

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
            UnsafeParallelHashMapData.DeallocateHashMap(m_Buffer, m_AllocatorLabel);
            m_Buffer = null;
        }

        /// <summary>
        /// Creates and schedules a job that will dispose this hash map.
        /// </summary>
        /// <param name="inputDeps">A job handle. The newly scheduled job will depend upon this handle.</param>
        /// <returns>The handle of a new job that will dispose this hash map.</returns>
        [NotBurstCompatible /* This is not burst compatible because of IJob's use of a static IntPtr. Should switch to IJobBurstSchedulable in the future */]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new UnsafeParallelHashMapDisposeJob { Data = m_Buffer, Allocator = m_AllocatorLabel }.Schedule(inputDeps);
            m_Buffer = null;
            return jobHandle;
        }

        /// <summary>
        /// Returns an array with a copy of all the keys (in no particular order).
        /// </summary>
        /// <remarks>A key with *N* values is included *N* times in the array.
        ///
        /// Use `GetUniqueKeyArray` of <see cref="Unity.Collections.NativeParallelHashMapExtensions"/> instead if you only want one occurrence of each key.</remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all the keys (in no particular order).</returns>
        public NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            var result = CollectionHelper.CreateNativeArray<TKey>(Count(), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeParallelHashMapData.GetKeyArray(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// Returns an array with a copy of all the values (in no particular order).
        /// </summary>
        /// <remarks>The values are not deduplicated. If you sort the returned array,
        /// you can use <see cref="Unity.Collections.NativeParallelHashMapExtensions.Unique{T}"/> to remove duplicate values.</remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all the values (in no particular order).</returns>
        public NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            var result = CollectionHelper.CreateNativeArray<TValue>(Count(), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeParallelHashMapData.GetValueArray(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// Returns a NativeKeyValueArrays with a copy of all the keys and values (in no particular order).
        /// </summary>
        /// <remarks>A key with *N* values is included *N* times in the array.
        /// </remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>A NativeKeyValueArrays with a copy of all the keys and values (in no particular order).</returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            var result = new NativeKeyValueArrays<TKey, TValue>(Count(), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeParallelHashMapData.GetKeyValueArrays(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// Returns an enumerator over the values of an individual key.
        /// </summary>
        /// <param name="key">The key to get an enumerator for.</param>
        /// <returns>An enumerator over the values of a key.</returns>
        public Enumerator GetValuesForKey(TKey key)
        {
            return new Enumerator { hashmap = this, key = key, isFirst = true };
        }

        /// <summary>
        /// An enumerator over the values of an individual key in a multi hash map.
        /// </summary>
        /// <remarks>
        /// In an enumerator's initial state, <see cref="Current"/> is not valid to read.
        /// The first <see cref="MoveNext"/> call advances the enumerator to the first value of the key.
        /// </remarks>
        public struct Enumerator : IEnumerator<TValue>
        {
            internal UnsafeMultiHashMap<TKey, TValue> hashmap;
            internal TKey key;
            internal bool isFirst;

            TValue value;
            NativeParallelMultiHashMapIterator<TKey> iterator;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next value of the key.
            /// </summary>
            /// <returns>True if <see cref="Current"/> is valid to read after the call.</returns>
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
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset() => isFirst = true;

            /// <summary>
            /// The current value.
            /// </summary>
            /// <value>The current value.</value>
            public TValue Current => value;

            object IEnumerator.Current => Current;

            /// <summary>
            /// Returns this enumerator.
            /// </summary>
            /// <returns>This enumerator.</returns>
            public Enumerator GetEnumerator() { return this; }
        }

        /// <summary>
        /// Returns a parallel writer for this hash map.
        /// </summary>
        /// <returns>A parallel writer for this hash map.</returns>
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
        /// A parallel writer for an UnsafeParallelMultiHashMap.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a UnsafeParallelMultiHashMap.
        /// </remarks>
        [NativeContainerIsAtomicWriteOnly]
        public unsafe struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeParallelHashMapData* m_Buffer;

            [NativeSetThreadIndex]
            internal int m_ThreadIndex;

            /// <summary>
            /// Returns the number of key-value pairs that fit in the current allocation.
            /// </summary>
            /// <value>The number of key-value pairs that fit in the current allocation.</value>
            public int Capacity
            {
                get
                {
                    return m_Buffer->keyCapacity;
                }
            }

            /// <summary>
            /// Adds a new key-value pair.
            /// </summary>
            /// <remarks>
            /// If a key-value pair with this key is already present, an additional separate key-value pair is added.
            /// </remarks>
            /// <param name="key">The key to add.</param>
            /// <param name="item">The value to add.</param>
            public void Add(TKey key, TValue item)
            {
                Assert.IsTrue(m_ThreadIndex >= 0);
                UnsafeParallelHashMapBase<TKey, TValue>.AddAtomicMulti(m_Buffer, key, item, m_ThreadIndex);
            }
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <remarks>A key with *N* values is visited by the enumerator *N* times.</remarks>
        /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
        public KeyValueEnumerator GetEnumerator()
        {
            return new KeyValueEnumerator { m_Enumerator = new UnsafeParallelHashMapDataEnumerator(m_Buffer) };
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<KeyValue<TKey, TValue>> IEnumerable<KeyValue<TKey, TValue>>.GetEnumerator()
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
        /// An enumerator over the key-value pairs of a multi hash map.
        /// </summary>
        /// <remarks>A key with *N* values is visited by the enumerator *N* times.
        ///
        /// In an enumerator's initial state, <see cref="Current"/> is not valid to read.
        /// The first <see cref="MoveNext"/> call advances the enumerator to the first key-value pair.
        /// </remarks>
        public struct KeyValueEnumerator : IEnumerator<KeyValue<TKey, TValue>>
        {
            internal UnsafeParallelHashMapDataEnumerator m_Enumerator;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next key-value pair.
            /// </summary>
            /// <returns>True if <see cref="Current"/> is valid to read after the call.</returns>
            public bool MoveNext() => m_Enumerator.MoveNext();

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset() => m_Enumerator.Reset();

            /// <summary>
            /// The current key-value pair.
            /// </summary>
            /// <value>The current key-value pair.</value>
            public KeyValue<TKey, TValue> Current => m_Enumerator.GetCurrent<TKey, TValue>();

            object IEnumerator.Current => Current;
        }
    }

    [Obsolete("UnsafeHashSet is renamed to UnsafeParallelHashSet. (UnityUpgradable) -> UnsafeParallelHashSet<T>", false)]
    public unsafe struct UnsafeHashSet<T>
        : INativeDisposable
        , IEnumerable<T>  // Used by collection initializers.
        where T : unmanaged, IEquatable<T>
    {
        internal UnsafeParallelHashMap<T, bool> m_Data;

        /// <summary>
        /// Initializes and returns an instance of UnsafeHashSet.
        /// </summary>
        /// <param name="capacity">The number of values that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
        public UnsafeHashSet(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_Data = new UnsafeParallelHashMap<T, bool>(capacity, allocator);
        }

        /// <summary>
        /// Whether this set is empty.
        /// </summary>
        /// <value>True if this set is empty.</value>
        public bool IsEmpty => m_Data.IsEmpty;

        /// <summary>
        /// Returns the current number of values in this set.
        /// </summary>
        /// <returns>The current number of values in this set.</returns>
        public int Count() => m_Data.Count();

        /// <summary>
        /// The number of values that fit in the current allocation.
        /// </summary>
        /// <value>The number of values that fit in the current allocation.</value>
        /// <param name="value">A new capacity. Must be larger than current capacity.</param>
        /// <exception cref="Exception">Thrown if `value` is less than the current capacity.</exception>
        public int Capacity { get => m_Data.Capacity; set => m_Data.Capacity = value; }

        /// <summary>
        /// Whether this set has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this set has been allocated (and not yet deallocated).</value>
        public bool IsCreated => m_Data.IsCreated;

        /// <summary>
        /// Releases all resources (memory).
        /// </summary>
        public void Dispose() => m_Data.Dispose();

        /// <summary>
        /// Creates and schedules a job that will dispose this set.
        /// </summary>
        /// <param name="inputDeps">A job handle. The newly scheduled job will depend upon this handle.</param>
        /// <returns>The handle of a new job that will dispose this set.</returns>
        [NotBurstCompatible /* This is not burst compatible because of IJob's use of a static IntPtr. Should switch to IJobBurstSchedulable in the future */]
        public JobHandle Dispose(JobHandle inputDeps) => m_Data.Dispose(inputDeps);

        /// <summary>
        /// Removes all values.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear() => m_Data.Clear();

        /// <summary>
        /// Adds a new value (unless it is already present).
        /// </summary>
        /// <param name="item">The value to add.</param>
        /// <returns>True if the value was not already present.</returns>
        public bool Add(T item) => m_Data.TryAdd(item, false);

        /// <summary>
        /// Removes a particular value.
        /// </summary>
        /// <param name="item">The value to remove.</param>
        /// <returns>True if the value was present.</returns>
        public bool Remove(T item) => m_Data.Remove(item);

        /// <summary>
        /// Returns true if a particular value is present.
        /// </summary>
        /// <param name="item">The value to check for.</param>
        /// <returns>True if the value was present.</returns>
        public bool Contains(T item) => m_Data.ContainsKey(item);

        /// <summary>
        /// Returns an array with a copy of this set's values (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of the set's values.</returns>
        public NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator) => m_Data.GetKeyArray(allocator);

        /// <summary>
        /// Returns a parallel writer.
        /// </summary>
        /// <returns>A parallel writer.</returns>
        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter { m_Data = m_Data.AsParallelWriter() };
        }

        /// <summary>
        /// A parallel writer for an UnsafeHashSet.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a set.
        /// </remarks>
        [NativeContainerIsAtomicWriteOnly]
        public struct ParallelWriter
        {
            internal UnsafeParallelHashMap<T, bool>.ParallelWriter m_Data;

            /// <summary>
            /// The number of values that fit in the current allocation.
            /// </summary>
            /// <value>The number of values that fit in the current allocation.</value>
            public int Capacity => m_Data.Capacity;

            /// <summary>
            /// Adds a new value (unless it is already present).
            /// </summary>
            /// <param name="item">The value to add.</param>
            /// <returns>True if the value is not already present.</returns>
            public bool Add(T item) => m_Data.TryAdd(item, false);
        }

        /// <summary>
        /// Returns an enumerator over the values of this set.
        /// </summary>
        /// <returns>An enumerator over the values of this set.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator { m_Enumerator = new UnsafeParallelHashMapDataEnumerator(m_Data.m_Buffer) };
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
            internal UnsafeParallelHashMapDataEnumerator m_Enumerator;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next value.
            /// </summary>
            /// <returns>True if `Current` is valid to read after the call.</returns>
            public bool MoveNext() => m_Enumerator.MoveNext();

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset() => m_Enumerator.Reset();

            /// <summary>
            /// The current value.
            /// </summary>
            /// <value>The current value.</value>
            public T Current => m_Enumerator.GetCurrentKey<T>();

            object IEnumerator.Current => Current;
        }
    }
}

namespace Unity.Collections
{
    [Obsolete("NativeMultiHashMapIterator is renamed to NativeParallelMultiHashMapIterator. (UnityUpgradable) -> NativeParallelMultiHashMapIterator<TKey>", false)]
    public struct NativeMultiHashMapIterator<TKey>
        where TKey : struct
    {
        internal TKey key;
        internal int NextEntryIndex;
        internal int EntryIndex;

        /// <summary>
        /// Returns the entry index.
        /// </summary>
        /// <returns>The entry index.</returns>
        public int GetEntryIndex() => EntryIndex;
    }

    [Obsolete("NativeHashMap is renamed to NativeParallelHashMap. (UnityUpgradable) -> NativeParallelHashMap<TKey, TValue>", false)]
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    public unsafe struct NativeHashMap<TKey, TValue>
        : INativeDisposable
        , IEnumerable<KeyValue<TKey, TValue>> // Used by collection initializers.
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        internal UnsafeParallelHashMap<TKey, TValue> m_HashMapData;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeHashMap<TKey, TValue>>();

#if REMOVE_DISPOSE_SENTINEL
#else
        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif
#endif

        /// <summary>
        /// Initializes and returns an instance of NativeParallelHashMap.
        /// </summary>
        /// <param name="capacity">The number of key-value pairs that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
        public NativeHashMap(int capacity, AllocatorManager.AllocatorHandle allocator)
            : this(capacity, allocator, 2)
        {
        }

        NativeHashMap(int capacity, AllocatorManager.AllocatorHandle allocator, int disposeSentinelStackDepth)
        {
            m_HashMapData = new UnsafeParallelHashMap<TKey, TValue>(capacity, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
#else
            if (AllocatorManager.IsCustomAllocator(allocator.ToAllocator))
            {
                m_Safety = AtomicSafetyHandle.Create();
                m_DisposeSentinel = null;
            }
            else
            {
                DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, disposeSentinelStackDepth, allocator.ToAllocator);
            }
#endif

            CollectionHelper.SetStaticSafetyId<NativeParallelHashMap<TKey, TValue>>(ref m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        /// <summary>
        /// Whether this hash map is empty.
        /// </summary>
        /// <value>True if this hash map is empty or if the map has not been constructed.</value>
        public bool IsEmpty
        {
            get
            {
                if (!IsCreated)
                {
                    return true;
                }

                CheckRead();
                return m_HashMapData.IsEmpty;
            }
        }

        /// <summary>
        /// The current number of key-value pairs in this hash map.
        /// </summary>
        /// <returns>The current number of key-value pairs in this hash map.</returns>
        public int Count()
        {
            CheckRead();
            return m_HashMapData.Count();
        }

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
                CheckRead();
                return m_HashMapData.Capacity;
            }

            set
            {
                CheckWrite();
                m_HashMapData.Capacity = value;
            }
        }

        /// <summary>
        /// Removes all key-value pairs.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            CheckWrite();
            m_HashMapData.Clear();
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
            CheckWrite();
            return m_HashMapData.TryAdd(key, item);
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
            var added = TryAdd(key, item);

            if (!added)
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
            CheckWrite();
            return m_HashMapData.Remove(key);
        }

        /// <summary>
        /// Returns the value associated with a key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="item">Outputs the value associated with the key. Outputs default if the key was not present.</param>
        /// <returns>True if the key was present.</returns>
        public bool TryGetValue(TKey key, out TValue item)
        {
            CheckRead();
            return m_HashMapData.TryGetValue(key, out item);
        }

        /// <summary>
        /// Returns true if a given key is present in this hash map.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>True if the key was present.</returns>
        public bool ContainsKey(TKey key)
        {
            CheckRead();
            return m_HashMapData.ContainsKey(key);
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
                CheckRead();

                TValue res;

                if (m_HashMapData.TryGetValue(key, out res))
                {
                    return res;
                }

                ThrowKeyNotPresent(key);

                return default;
            }

            set
            {
                CheckWrite();
                m_HashMapData[key] = value;
            }
        }

        /// <summary>
        /// Whether this hash map has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this hash map has been allocated (and not yet deallocated).</value>
        public bool IsCreated => m_HashMapData.IsCreated;

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#else
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
#endif
            m_HashMapData.Dispose();
        }

        /// <summary>
        /// Creates and schedules a job that will dispose this hash map.
        /// </summary>
        /// <param name="inputDeps">A job handle. The newly scheduled job will depend upon this handle.</param>
        /// <returns>The handle of a new job that will dispose this hash map.</returns>
        [NotBurstCompatible /* This is not burst compatible because of IJob's use of a static IntPtr. Should switch to IJobBurstSchedulable in the future */]
        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
#else
            // [DeallocateOnJobCompletion] is not supported, but we want the deallocation
            // to happen in a thread. DisposeSentinel needs to be cleared on main thread.
            // AtomicSafetyHandle can be destroyed after the job was scheduled (Job scheduling
            // will check that no jobs are writing to the container).
            DisposeSentinel.Clear(ref m_DisposeSentinel);
#endif

            var jobHandle = new UnsafeParallelHashMapDataDisposeJob { Data = new UnsafeParallelHashMapDataDispose { m_Buffer = m_HashMapData.m_Buffer, m_AllocatorLabel = m_HashMapData.m_AllocatorLabel, m_Safety = m_Safety } }.Schedule(inputDeps);

            AtomicSafetyHandle.Release(m_Safety);
#else
            var jobHandle = new UnsafeParallelHashMapDataDisposeJob { Data = new UnsafeParallelHashMapDataDispose { m_Buffer = m_HashMapData.m_Buffer, m_AllocatorLabel = m_HashMapData.m_AllocatorLabel }  }.Schedule(inputDeps);
#endif
            m_HashMapData.m_Buffer = null;

            return jobHandle;
        }

        /// <summary>
        /// Returns an array with a copy of all this hash map's keys (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all this hash map's keys (in no particular order).</returns>
        public NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            CheckRead();
            return m_HashMapData.GetKeyArray(allocator);
        }

        /// <summary>
        /// Returns an array with a copy of all this hash map's values (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all this hash map's values (in no particular order).</returns>
        public NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            CheckRead();
            return m_HashMapData.GetValueArray(allocator);
        }

        /// <summary>
        /// Returns a NativeKeyValueArrays with a copy of all this hash map's keys and values.
        /// </summary>
        /// <remarks>The key-value pairs are copied in no particular order. For all `i`, `Values[i]` will be the value associated with `Keys[i]`.</remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>A NativeKeyValueArrays with a copy of all this hash map's keys and values.</returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            CheckRead();
            return m_HashMapData.GetKeyValueArrays(allocator);
        }

        /// <summary>
        /// Returns a parallel writer for this hash map.
        /// </summary>
        /// <returns>A parallel writer for this hash map.</returns>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;
            writer.m_Writer = m_HashMapData.AsParallelWriter();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            writer.m_Safety = m_Safety;
            CollectionHelper.SetStaticSafetyId<ParallelWriter>(ref writer.m_Safety, ref ParallelWriter.s_staticSafetyId.Data);
#endif
            return writer;
        }

        /// <summary>
        /// A parallel writer for a NativeParallelHashMap.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a NativeParallelHashMap.
        /// </remarks>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        [DebuggerDisplay("Capacity = {m_Writer.Capacity}")]
        public unsafe struct ParallelWriter
        {
            internal UnsafeParallelHashMap<TKey, TValue>.ParallelWriter m_Writer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ParallelWriter>();
#endif
            /// <summary>
            /// Returns the index of the current thread.
            /// </summary>
            /// <remarks>In a job, each thread gets its own copy of the ParallelWriter struct, and the job system assigns
            /// each copy the index of its thread.</remarks>
            /// <value>The index of the current thread.</value>
            public int m_ThreadIndex => m_Writer.m_ThreadIndex;

            /// <summary>
            /// The number of key-value pairs that fit in the current allocation.
            /// </summary>
            /// <value>The number of key-value pairs that fit in the current allocation.</value>
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
            /// Adds a new key-value pair.
            /// </summary>
            /// <remarks>If the key is already present, this method returns false without modifying this hash map.</remarks>
            /// <param name="key">The key to add.</param>
            /// <param name="item">The value to add.</param>
            /// <returns>True if the key-value pair was added.</returns>
            public bool TryAdd(TKey key, TValue item)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
                return m_Writer.TryAdd(key, item);
            }
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
        public Enumerator GetEnumerator()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Safety);
            var ash = m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref ash);
#endif
            return new Enumerator
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = ash,
#endif
                m_Enumerator = new UnsafeParallelHashMapDataEnumerator(m_HashMapData.m_Buffer),
            };
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<KeyValue<TKey, TValue>> IEnumerable<KeyValue<TKey, TValue>>.GetEnumerator()
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
        /// An enumerator over the key-value pairs of a hash map.
        /// </summary>
        /// <remarks>
        /// In an enumerator's initial state, <see cref="Current"/> is not valid to read.
        /// From this state, the first <see cref="MoveNext"/> call advances the enumerator to the first key-value pair.
        /// </remarks>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public struct Enumerator : IEnumerator<KeyValue<TKey, TValue>>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
#endif
            internal UnsafeParallelHashMapDataEnumerator m_Enumerator;

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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Enumerator.MoveNext();
            }

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                m_Enumerator.Reset();
            }

            /// <summary>
            /// The current key-value pair.
            /// </summary>
            /// <value>The current key-value pair.</value>
            public KeyValue<TKey, TValue> Current => m_Enumerator.GetCurrent<TKey, TValue>();

            object IEnumerator.Current => Current;
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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ThrowKeyNotPresent(TKey key)
        {
            throw new ArgumentException($"Key: {key} is not present in the NativeParallelHashMap.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ThrowKeyAlreadyAdded(TKey key)
        {
            throw new ArgumentException("An item with the same key has already been added", nameof(key));
        }
    }

    [Obsolete("NativeMultiHashMap is renamed to NativeParallelMultiHashMap. (UnityUpgradable) -> NativeParallelMultiHashMap<TKey, TValue>", false)]
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    public unsafe struct NativeMultiHashMap<TKey, TValue>
        : INativeDisposable
        , IEnumerable<KeyValue<TKey, TValue>> // Used by collection initializers.
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        internal UnsafeParallelMultiHashMap<TKey, TValue> m_MultiHashMapData;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeMultiHashMap<TKey, TValue>>();

#if REMOVE_DISPOSE_SENTINEL
#else
        [NativeSetClassTypeToNullOnSchedule]
        internal DisposeSentinel m_DisposeSentinel;
#endif
#endif

        /// <summary>
        /// Returns a newly allocated multi hash map.
        /// </summary>
        /// <param name="capacity">The number of key-value pairs that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
        public NativeMultiHashMap(int capacity, AllocatorManager.AllocatorHandle allocator)
            : this(capacity, allocator, 2)
        {
        }

        internal void Initialize<U>(int capacity, ref U allocator, int disposeSentinelStackDepth)
            where U : unmanaged, AllocatorManager.IAllocator
        {
            m_MultiHashMapData = new UnsafeParallelMultiHashMap<TKey, TValue>(capacity, allocator.Handle);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
#else
            if (allocator.IsCustomAllocator)
            {
                m_Safety = AtomicSafetyHandle.Create();
                m_DisposeSentinel = null;
            }
            else
            {
                DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, disposeSentinelStackDepth, allocator.ToAllocator);
            }
#endif

            CollectionHelper.SetStaticSafetyId<NativeMultiHashMap<TKey, TValue>>(ref m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        NativeMultiHashMap(int capacity, AllocatorManager.AllocatorHandle allocator, int disposeSentinelStackDepth)
        {
            this = default;
            Initialize(capacity, ref allocator, disposeSentinelStackDepth);
        }

        /// <summary>
        /// Whether this hash map is empty.
        /// </summary>
        /// <value>True if the hash map is empty or if the hash map has not been constructed.</value>
        public bool IsEmpty
        {
            get
            {
                CheckRead();
                return m_MultiHashMapData.IsEmpty;
            }
        }

        /// <summary>
        /// Returns the current number of key-value pairs in this hash map.
        /// </summary>
        /// <remarks>Key-value pairs with matching keys are counted as separate, individual pairs.</remarks>
        /// <returns>The current number of key-value pairs in this hash map.</returns>
        public int Count()
        {
            CheckRead();
            return m_MultiHashMapData.Count();
        }

        /// <summary>
        /// Returns the number of key-value pairs that fit in the current allocation.
        /// </summary>
        /// <value>The number of key-value pairs that fit in the current allocation.</value>
        /// <param name="value">A new capacity. Must be larger than the current capacity.</param>
        /// <exception cref="Exception">Thrown if `value` is less than the current capacity.</exception>
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
        /// Removes all key-value pairs.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            CheckWrite();
            m_MultiHashMapData.Clear();
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <remarks>
        /// If a key-value pair with this key is already present, an additional separate key-value pair is added.
        /// </remarks>
        /// <param name="key">The key to add.</param>
        /// <param name="item">The value to add.</param>
        public void Add(TKey key, TValue item)
        {
            CheckWrite();
            m_MultiHashMapData.Add(key, item);
        }

        /// <summary>
        /// Removes a key and its associated value(s).
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>The number of removed key-value pairs. If the key was not present, returns 0.</returns>
        public int Remove(TKey key)
        {
            CheckWrite();
            return m_MultiHashMapData.Remove(key);
        }

        /// <summary>
        /// Removes a single key-value pair.
        /// </summary>
        /// <param name="it">An iterator representing the key-value pair to remove.</param>
        /// <exception cref="InvalidOperationException">Thrown if the iterator is invalid.</exception>
        public void Remove(NativeParallelMultiHashMapIterator<TKey> it)
        {
            CheckWrite();
            m_MultiHashMapData.Remove(it);
        }

        /// <summary>
        /// Gets an iterator for a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="item">Outputs the associated value represented by the iterator.</param>
        /// <param name="it">Outputs an iterator.</param>
        /// <returns>True if the key was present.</returns>
        public bool TryGetFirstValue(TKey key, out TValue item, out NativeParallelMultiHashMapIterator<TKey> it)
        {
            CheckRead();
            return m_MultiHashMapData.TryGetFirstValue(key, out item, out it);
        }

        /// <summary>
        /// Advances an iterator to the next value associated with its key.
        /// </summary>
        /// <param name="item">Outputs the next value.</param>
        /// <param name="it">A reference to the iterator to advance.</param>
        /// <returns>True if the key was present and had another value.</returns>
        public bool TryGetNextValue(out TValue item, ref NativeParallelMultiHashMapIterator<TKey> it)
        {
            CheckRead();
            return m_MultiHashMapData.TryGetNextValue(out item, ref it);
        }

        /// <summary>
        /// Returns true if a given key is present in this hash map.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>True if the key was present in this hash map.</returns>
        public bool ContainsKey(TKey key)
        {
            return TryGetFirstValue(key, out var temp0, out var temp1);
        }

        /// <summary>
        /// Returns the number of values associated with a given key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>The number of values associated with the key. Returns 0 if the key was not present.</returns>
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
        /// Sets a new value for an existing key-value pair.
        /// </summary>
        /// <param name="item">The new value.</param>
        /// <param name="it">The iterator representing a key-value pair.</param>
        /// <returns>True if a value was overwritten.</returns>
        public bool SetValue(TValue item, NativeParallelMultiHashMapIterator<TKey> it)
        {
            CheckWrite();
            return m_MultiHashMapData.SetValue(item, it);
        }

        /// <summary>
        /// Whether this hash map has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this hash map has been allocated (and not yet deallocated).</value>
        public bool IsCreated => m_MultiHashMapData.IsCreated;

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#else
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
#endif
            m_MultiHashMapData.Dispose();
        }

        /// <summary>
        /// Creates and schedules a job that will dispose this hash map.
        /// </summary>
        /// <param name="inputDeps">A job handle. The newly scheduled job will depend upon this handle.</param>
        /// <returns>The handle of a new job that will dispose this hash map.</returns>
        [NotBurstCompatible /* This is not burst compatible because of IJob's use of a static IntPtr. Should switch to IJobBurstSchedulable in the future */]
        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
#else
            // [DeallocateOnJobCompletion] is not supported, but we want the deallocation
            // to happen in a thread. DisposeSentinel needs to be cleared on main thread.
            // AtomicSafetyHandle can be destroyed after the job was scheduled (Job scheduling
            // will check that no jobs are writing to the container).
            DisposeSentinel.Clear(ref m_DisposeSentinel);
#endif
            var jobHandle = new UnsafeParallelHashMapDataDisposeJob { Data = new UnsafeParallelHashMapDataDispose { m_Buffer = m_MultiHashMapData.m_Buffer, m_AllocatorLabel = m_MultiHashMapData.m_AllocatorLabel, m_Safety = m_Safety } }.Schedule(inputDeps);

            AtomicSafetyHandle.Release(m_Safety);
#else
            var jobHandle = new UnsafeParallelHashMapDataDisposeJob { Data = new UnsafeParallelHashMapDataDispose { m_Buffer = m_MultiHashMapData.m_Buffer, m_AllocatorLabel = m_MultiHashMapData.m_AllocatorLabel } }.Schedule(inputDeps);
#endif
            m_MultiHashMapData.m_Buffer = null;

            return jobHandle;
        }

        /// <summary>
        /// Returns an array with a copy of all the keys (in no particular order).
        /// </summary>
        /// <remarks>A key with *N* values is included *N* times in the array.
        ///
        /// Use `GetUniqueKeyArray` of <see cref="Unity.Collections.NativeParallelHashMapExtensions"/> instead if you only want one occurrence of each key.</remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all the keys (in no particular order).</returns>
        public NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            CheckRead();
            return m_MultiHashMapData.GetKeyArray(allocator);
        }

        /// <summary>
        /// Returns an array with a copy of all the values (in no particular order).
        /// </summary>
        /// <remarks>The values are not deduplicated. If you sort the returned array,
        /// you can use <see cref="Unity.Collections.NativeParallelHashMapExtensions.Unique{T}"/> to remove duplicate values.</remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all the values (in no particular order).</returns>
        public NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            CheckRead();
            return m_MultiHashMapData.GetValueArray(allocator);
        }

        /// <summary>
        /// Returns a NativeKeyValueArrays with a copy of all the keys and values (in no particular order).
        /// </summary>
        /// <remarks>A key with *N* values is included *N* times in the array.
        /// </remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>A NativeKeyValueArrays with a copy of all the keys and values (in no particular order).</returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            CheckRead();
            return m_MultiHashMapData.GetKeyValueArrays(allocator);
        }

        /// <summary>
        /// Returns a parallel writer for this hash map.
        /// </summary>
        /// <returns>A parallel writer for this hash map.</returns>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;
            writer.m_Writer = m_MultiHashMapData.AsParallelWriter();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            writer.m_Safety = m_Safety;
            CollectionHelper.SetStaticSafetyId<ParallelWriter>(ref writer.m_Safety, ref s_staticSafetyId.Data);
#endif
            return writer;
        }

        /// <summary>
        /// A parallel writer for a NativeParallelMultiHashMap.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a NativeParallelMultiHashMap.
        /// </remarks>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public unsafe struct ParallelWriter
        {
            internal UnsafeParallelMultiHashMap<TKey, TValue>.ParallelWriter m_Writer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ParallelWriter>();
#endif
            /// <summary>
            /// Returns the index of the current thread.
            /// </summary>
            /// <remarks>In a job, each thread gets its own copy of the ParallelWriter struct, and the job system assigns
            /// each copy the index of its thread.</remarks>
            /// <value>The index of the current thread.</value>
            public int m_ThreadIndex => m_Writer.m_ThreadIndex;

            /// <summary>
            /// Returns the number of key-value pairs that fit in the current allocation.
            /// </summary>
            /// <value>The number of key-value pairs that fit in the current allocation.</value>
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
            /// Adds a new key-value pair.
            /// </summary>
            /// <remarks>
            /// If a key-value pair with this key is already present, an additional separate key-value pair is added.
            /// </remarks>
            /// <param name="key">The key to add.</param>
            /// <param name="item">The value to add.</param>
            public void Add(TKey key, TValue item)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
                m_Writer.Add(key, item);
            }
        }

        /// <summary>
        /// Returns an enumerator over the values of an individual key.
        /// </summary>
        /// <param name="key">The key to get an enumerator for.</param>
        /// <returns>An enumerator over the values of a key.</returns>
        public Enumerator GetValuesForKey(TKey key)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return new Enumerator { hashmap = this, key = key, isFirst = true };
        }

        /// <summary>
        /// An enumerator over the values of an individual key in a multi hash map.
        /// </summary>
        /// <remarks>
        /// In an enumerator's initial state, <see cref="Current"/> is not valid to read.
        /// The first <see cref="MoveNext"/> call advances the enumerator to the first value of the key.
        /// </remarks>
        public struct Enumerator : IEnumerator<TValue>
        {
            internal NativeMultiHashMap<TKey, TValue> hashmap;
            internal TKey key;
            internal bool isFirst;

            TValue value;
            NativeParallelMultiHashMapIterator<TKey> iterator;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next value of the key.
            /// </summary>
            /// <returns>True if <see cref="Current"/> is valid to read after the call.</returns>
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
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset() => isFirst = true;

            /// <summary>
            /// The current value.
            /// </summary>
            /// <value>The current value.</value>
            public TValue Current => value;

            object IEnumerator.Current => Current;

            /// <summary>
            /// Returns this enumerator.
            /// </summary>
            /// <returns>This enumerator.</returns>
            public Enumerator GetEnumerator() { return this; }
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <remarks>A key with *N* values is visited by the enumerator *N* times.</remarks>
        /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
        public KeyValueEnumerator GetEnumerator()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Safety);
            var ash = m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref ash);
#endif
            return new KeyValueEnumerator
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = ash,
#endif
                m_Enumerator = new UnsafeParallelHashMapDataEnumerator(m_MultiHashMapData.m_Buffer),
            };
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<KeyValue<TKey, TValue>> IEnumerable<KeyValue<TKey, TValue>>.GetEnumerator()
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
        /// An enumerator over the key-value pairs of a multi hash map.
        /// </summary>
        /// <remarks>A key with *N* values is visited by the enumerator *N* times.
        ///
        /// In an enumerator's initial state, <see cref="Current"/> is not valid to read.
        /// The first <see cref="MoveNext"/> call advances the enumerator to the first key-value pair.
        /// </remarks>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public struct KeyValueEnumerator : IEnumerator<KeyValue<TKey, TValue>>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
#endif
            internal UnsafeParallelHashMapDataEnumerator m_Enumerator;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next key-value pair.
            /// </summary>
            /// <returns>True if <see cref="Current"/> is valid to read after the call.</returns>
            public unsafe bool MoveNext()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Enumerator.MoveNext();
            }

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                m_Enumerator.Reset();
            }

            /// <summary>
            /// The current key-value pair.
            /// </summary>
            /// <value>The current key-value pair.</value>
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

            object IEnumerator.Current => Current;
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

    [Obsolete("NativeHashSet is renamed to NativeParallelHashSet. (UnityUpgradable) -> NativeParallelHashSet<T>", false)]
    public unsafe struct NativeHashSet<T>
        : INativeDisposable
        , IEnumerable<T> // Used by collection initializers.
        where T : unmanaged, IEquatable<T>
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeHashSet<T>>();
#endif

        internal NativeParallelHashMap<T, bool> m_Data;

        /// <summary>
        /// Initializes and returns an instance of NativeHashSet.
        /// </summary>
        /// <param name="capacity">The number of values that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
        public NativeHashSet(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_Data = new NativeParallelHashMap<T, bool>(capacity, allocator);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.SetStaticSafetyId<NativeHashSet<T>>(ref m_Data.m_Safety, ref s_staticSafetyId.Data);
#endif
        }

        /// <summary>
        /// Whether this set is empty.
        /// </summary>
        /// <value>True if this set is empty or if the set has not been constructed.</value>
        public bool IsEmpty => m_Data.IsEmpty;

        /// <summary>
        /// Returns the current number of values in this set.
        /// </summary>
        /// <returns>The current number of values in this set.</returns>
        public int Count() => m_Data.Count();

        /// <summary>
        /// The number of values that fit in the current allocation.
        /// </summary>
        /// <value>The number of values that fit in the current allocation.</value>
        /// <param name="value">A new capacity. Must be larger than current capacity.</param>
        /// <exception cref="Exception">Thrown if `value` is less than the current capacity.</exception>
        public int Capacity { get => m_Data.Capacity; set => m_Data.Capacity = value; }

        /// <summary>
        /// Whether this set has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this set has been allocated (and not yet deallocated).</value>
        public bool IsCreated => m_Data.IsCreated;

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose() => m_Data.Dispose();

        /// <summary>
        /// Creates and schedules a job that will dispose this set.
        /// </summary>
        /// <param name="inputDeps">A job handle. The newly scheduled job will depend upon this handle.</param>
        /// <returns>The handle of a new job that will dispose this set.</returns>
        [NotBurstCompatible /* This is not burst compatible because of IJob's use of a static IntPtr. Should switch to IJobBurstSchedulable in the future */]
        public JobHandle Dispose(JobHandle inputDeps) => m_Data.Dispose(inputDeps);

        /// <summary>
        /// Removes all values.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear() => m_Data.Clear();

        /// <summary>
        /// Adds a new value (unless it is already present).
        /// </summary>
        /// <param name="item">The value to add.</param>
        /// <returns>True if the value was not already present.</returns>
        public bool Add(T item) => m_Data.TryAdd(item, false);

        /// <summary>
        /// Removes a particular value.
        /// </summary>
        /// <param name="item">The value to remove.</param>
        /// <returns>True if the value was present.</returns>
        public bool Remove(T item) => m_Data.Remove(item);

        /// <summary>
        /// Returns true if a particular value is present.
        /// </summary>
        /// <param name="item">The value to check for.</param>
        /// <returns>True if the value was present.</returns>
        public bool Contains(T item) => m_Data.ContainsKey(item);

        /// <summary>
        /// Returns an array with a copy of this set's values (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of the set's values.</returns>
        public NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator) => m_Data.GetKeyArray(allocator);

        /// <summary>
        /// Returns a parallel writer.
        /// </summary>
        /// <returns>A parallel writer.</returns>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;
            writer.m_Data = m_Data.AsParallelWriter();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.SetStaticSafetyId<ParallelWriter>(ref writer.m_Data.m_Safety, ref ParallelWriter.s_staticSafetyId.Data);
#endif
            return writer;
        }

        /// <summary>
        /// A parallel writer for a NativeHashSet.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a set.
        /// </remarks>
        [NativeContainerIsAtomicWriteOnly]
        public struct ParallelWriter
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ParallelWriter>();
#endif
            internal NativeParallelHashMap<T, bool>.ParallelWriter m_Data;

            /// <summary>
            /// The number of values that fit in the current allocation.
            /// </summary>
            /// <value>The number of values that fit in the current allocation.</value>
            public int Capacity => m_Data.Capacity;

            /// <summary>
            /// Adds a new value (unless it is already present).
            /// </summary>
            /// <param name="item">The value to add.</param>
            /// <returns>True if the value is not already present.</returns>
            public bool Add(T item) => m_Data.TryAdd(item, false);
        }

        /// <summary>
        /// Returns an enumerator over the values of this set.
        /// </summary>
        /// <returns>An enumerator over the values of this set.</returns>
        public Enumerator GetEnumerator()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Data.m_Safety);
            var ash = m_Data.m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref ash);
#endif
            return new Enumerator
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = ash,
#endif
                m_Enumerator = new UnsafeParallelHashMapDataEnumerator(m_Data.m_HashMapData.m_Buffer),
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
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public struct Enumerator : IEnumerator<T>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
#endif
            internal UnsafeParallelHashMapDataEnumerator m_Enumerator;

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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Enumerator.MoveNext();
            }

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                m_Enumerator.Reset();
            }

            /// <summary>
            /// The current value.
            /// </summary>
            /// <value>The current value.</value>
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

}
