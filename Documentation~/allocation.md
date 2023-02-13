---
uid: collections-allocation
---

# Use allocators to control unmanaged memory

The Collections package allocates `Native-` and `Unsafe-` collections from unmanaged memory, which means that their existence is unknown to the garbage collector.

You are responsible for deallocating any unmanaged memory that you don't need. If you fail to deallocate large or numerous allocations, it can lead to wasting a lot of memory, which might slow down or crash your program.

## Allocator overview

An **allocator** governs some unmanaged memory from which you can make allocations. Different allocators organize and track their memory in different ways. The Collections package includes the following allocators:

* [`Allocator.Temp`](#allocatortemp): The fastest allocator, for short-lived allocations. You can't pass this allocator to a job.
* [`Allocator.TempJob`](#allocatortempjob): A short-lived allocator that you can pass into jobs.
* [`Allocator.Persistent`](#allocatorpersistent): The slowest allocator for indefinite lifetime allocations. You can pass this allocator to a job.

### Allocator.Temp

Each frame, the main thread creates a Temp allocator which it deallocates in its entirety at the end of the frame. Each job also creates one Temp allocator per thread, and deallocates them in their entirety at the end of the job. Because a Temp allocator gets discarded as a whole, you don't need to manually deallocate Temp allocations, and doing so does nothing.  The minimum alignment of Temp allocations is 64 bytes.

Temp allocations are only safe to use within the thread and within the scope where they were allocated. While you can make Temp allocations within a job, you can't pass main thread Temp allocations into a job. For example, you can't pass a NativeArray that's Temp allocated in the main thread into a job.

### Allocator.TempJob

You must deallocate TempJob allocations within 4 frames of their creation. 4 frames is the limit so that you can have an allocation that lasts a couple of frames with some extra margin for error.  The minimum alignment of TempJob allocations is 16 bytes.

For `Native-` collection types, the disposal safety checks throw an exception if a TempJob allocation lasts longer than 4 frames. For `Unsafe-` collection types, you must deallocate them within 4 frames, but Unity doesn't perform any safety checks to ensure you do so.

   
### Allocator.Persistent

Because Persistent allocations can remain indefinitely, safety checks can't detect if a Persistent allocation has outlived its intended lifetime. As such, you should be extra careful to deallocate a Persistent allocation when you no longer need it.  The minimum alignment of Persistent allocations is 16 bytes.

## Deallocating an allocator

Each collection retains a reference to the allocator that allocated its memory. This is because you must specify the allocator to deallocate its memory.

* An `Unsafe-` collection's `Dispose` method deallocates its memory.
* A `Native-` collection's `Dispose` method deallocates its memory and frees the handles needed for safety checks. 
* An enumerator's `Dispose` method does nothing. The method exists only to fulfill the `IEnumerator<T>` interface.

To dispose a collection after the jobs which need it have run, you can use the `Dispose(JobHandle)` method. This creates and schedules a job which disposes of the collection, and this new job takes the input handle as its dependency. Effectively, the method defers disposal until after the dependency runs:

[!code-cs[allocation_dispose_job](../DocCodeSamples.Tests/CollectionsAllocationExamples.cs#allocation_dispose_job)]

### IsCreated property

The `IsCreated` property of a collection is false only in the following cases:

* Immediately after creating a collection with its default constructor.
* After `Dispose` has been called on the collection.

Understand, however, that you don't need to use a collections's default constructor. It's only made available because C# requires all structs have a public default constructor.

Calling `Dispose` on a collection sets `IsCreated` to false only for that struct, and not in any copies of the struct. `IsCreated` might still be true even after the collection's underlying memory is deallocated in the following situations:

* `Dispose` was called on a different copy of the struct.
* The underlying memory was deallocated via an [alias](#aliasing).

## Aliasing

An **alias** is a collection which doesn't have its own allocation but instead shares the allocation of another collection, in whole or in part. For example, you can create an `UnsafeList` that doesn't allocate its own memory but instead uses a `NativeList`'s allocation. Writing to this shared memory via the `UnsafeList` affects the content of the `NativeList`, and vice versa.

You don't need to dispose aliases, and calling `Dispose` on an alias does nothing. Once an original is disposed, you can no longer use the aliases of the original:

[!code-cs[allocation_aliasing](../DocCodeSamples.Tests/CollectionsAllocationExamples.cs#allocation_aliasing)]

Aliasing is useful for the following situations:

* Getting a collection's data in the form of another collection type without copying the data. For example, you can create an `UnsafeList` that aliases a `NativeArray`.
* Getting a subrange of a collection's data without copying the data. For example, you can create an UnsafeList that aliases a subrange of another list or array.   
* [Array reinterpretation](#array-reinterpretation).

An `Unsafe-` collection can alias a `Native-` collection even though such cases undermine the safety checks. For example, if an `UnsafeList` aliases a `NativeList`, it's not safe to schedule a job that accesses one while also another job is scheduled that accesses the other, but the safety checks don't catch these cases.

### Array reinterpretation

A **reinterpretation** of an array is an alias of the array that reads and writes the content as a different element type. For example, a `NativeArray<int>` which reinterprets a `NativeArray<ushort>` shares the same bytes, but it reads and writes the bytes as an int instead of a ushort. This is because each int is 4 bytes while each ushort is 2 bytes. Each int corresponds to two ushorts, and the reinterpretation has half the length of the original.

[!code-cs[allocation_reinterpretation](../DocCodeSamples.Tests/CollectionsAllocationExamples.cs#allocation_reinterpretation)]

## Further information

* [Define a custom allocator](allocator-custom-define.md)
* [Rewindable allocators](allocator-rewindable.md)