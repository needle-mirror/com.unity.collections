# Allocator benchmarks

The Collections package has different allocators that you can use to manage memory allocations. The different allocators organize and track their memory in different ways. These are the allocators available:

* [Allocator.Temp](allocation.md#allocatortemp): A fast allocator for short-lived allocations, which is created on every thread.
* [Allocator.TempJob](allocation.md#allocatortempjob): A short-lived allocator, which must be deallocated within 4 frames of their creation.
* [Allocator.Persistent](allocation.md#allocatorpersistent): The slowest allocator for indefinite lifetime allocations.
* [Rewindable allocator](allocator-rewindable.md): A custom allocator that is fast and thread safe, and can rewind and free all your allocations at one point.

The [Entities package](https://docs.unity3d.com/Packages/com.unity.entities@latest) has its own set of custom prebuilt allocators:

* [World update allocator](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/manual/allocators-world-update.html): A double rewindable allocator that a world owns, which is fast and thread safe.
* [Entity command buffer allocator](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/manual/allocators-entity-command-buffer.html): A rewindable allocator that an entity command buffer system owns and uses to create entity command buffers.
* [System group allocator](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/manual/allocators-system-group.html): An optional double rewindable allocator that a component system group creates when setting its rate manager. It's for allocations in a system of fixed or variable rate system group that ticks at different rate from the world update. 

For more information, see the Entities documentation on [Custom prebuilt allocators](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/manual/allocators-custom-prebuilt.html).

## Allocator feature comparison

The different allocators have the following different features:

|**Allocator type**|**Custom Allocator**|**Need to create before use**|**Lifetime**|**Automatically freed allocations**|**Can pass to jobs**|**Min Allocation Alignment (bytes)**|
|---|---|---|---|---|---|---|
|[Allocator.Temp](allocation.md#allocatortemp)|No|No|A frame or a job|Yes|No|64|
|[Allocator.TempJob](allocation.md#allocatortempjob)|No|No|Within 4 frames of creation|No|Yes|16|
|[Allocator.Persistent](allocation.md#allocatorpersistent)|No|No|Indefinite|No|Yes|16|
|[Rewindable allocator](allocator-rewindable.md)|Yes|Yes|Indefinite|No|Yes|64|

## Performance test results

The following performance tests compare Temp, TempJob, Persistent and rewindable allocators. Because the world update allocator, entity command buffer allocator, and system group allocator are rewindable allocators, their performance is reflected in the rewindable allocator test results. The allocators are tested in single thread cases and in multithread cases by scheduling allocations in jobs across all the cores.  

### Performance test results of single thread allocations

This performance test takes 100 measurements in a row of 150 allocations in a single IJob job. Each allocates the following memory: 

* Fixed small size (1KB)
* Fixed large size (1MB)
* Incremental size from 64KB to 150 * 64KB 
* Decremental size from 150 * 64KB to 64KB  

[The performance test results of single thread allocations](allocator-performance-results.md#Performancetestresultsofsinglethreadallocations) are measured in milliseconds. 

The results show that in single thread allocations, for fixed size small allocations, Temp allocator is the fastest, slightly faster than rewindable allocator, followed by TempJob allocator and then Persistant allocator. 

For other allocation sizes, rewindable allocator is the fastest, followed by TempJob, Temp allocator, and the Persistent allocator is the slowest.

### Performance results of multithreaded allocations

This performance test takes 100 measurements in a row of 150 allocations in every IJobParallelFor job across all CPU cores. Each allocates the following memory:

* Fixed small size (1KB)
* Fixed large size (1MB)
* Incremental size from 64KB to 150 * 64KB 
* Decremental size from 150 * 64KB to 64KB  

[The performance test results of of multithreaded allocations](allocator-performance-results.md#Performanceresultsofmultithreadedallocations) are measured in milliseconds. 

The results show that in multithreaded allocations, with varying allocation sizes, rewindable allocator is the fastest, followed by Temp allocator, Persistent allocator, and then TempJob allocator.  

For fixed size large allocations, rewindable allocator is the fastest, followed by Temp allocator, TempJob Allocator, and then Persistent allocator.  

For fixed size small allocations, rewindable allocator is the fastest followed by TempJob allocator, Temp allocator, and then Persistant allocator.
