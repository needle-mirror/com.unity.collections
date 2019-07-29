# Change log

## [0.1.0-preview] - 2019-07-30

### New Features

* NativeMultiHashMap.Remove(key, value) has been addded. It lets you remove
  all key & value pairs from the hashmap.
* Added ability to dispose containers from job (DisposeJob).
* Added UnsafeList.AddNoResize, and UnsafeList.AddRangeNoResize.

### Upgrade guide

* `Native*.Concurrent` is renamed to `Native*.ParallelWriter`.
* `Native*.ToConcurrent()` function is renamed to `Native*.AsParallelWriter()`.

### Changes

* Deprecated ToConcurrent, added AsParallelWriter instead.
* Allocator is not an optional argument anymore, user must always specify the allocator.
* Added Allocator to Unsafe\*List container, and removed per method allocator argument.
* Introduced memory intialization (NativeArrayOptions) argument to Unsafe\*List constructor and Resize.

### Fixes

* Fixed UnsafeList.RemoveRangeSwapBack when removing elements near the end of UnsafeList.
* Fixed safety handle use in NativeList.AddRange.


## [0.0.9-preview.20] - 2019-05-24

### Changes

* Updated dependencies for `Unity.Collections.Tests`


## [0.0.9-preview.19] - 2019-05-16

### New Features

* JobHandle NativeList.Dispose(JobHandle dependency) allows Disposing the container from a job.
* Exposed unsafe NativeSortExtension.Sort(T* array, int length) method for simpler sorting of unsafe arrays
* Imporoved documentation for `NativeList`
* Added `CollectionHelper.WriteLayout` debug utility

### Fixes

* Fixes a `NativeQueue` alignment issue.


## [0.0.9-preview.18] - 2019-05-01

Change tracking started with this version.

<!-- Template for version sections
## [0.0.0-preview.0]

### New Features


### Upgrade guide


### Changes


### Fixes
-->
