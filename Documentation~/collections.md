---
uid: collections-package
---
# Unity Collections Package

A C# collections library providing data structures that can be used in jobs, and
optimized by Burst compiler.

## Data structures

The Unity.Collections package includes the following data structures:

Data structure          | Description | Documentation
----------------------- | ----------- | -------------
`BitField32`            | Fixed size 32-bit array of bits. | @Unity.Collections.BitField32
`BitField64`            | Fixed size 64-bit array of bits. | @Unity.Collections.BitField64
`NativeBitArray`        | Arbitrary sized array of bits.   | @Unity.Collections.NativeBitArray
`UnsafeBitArray`        | Arbitrary sized array of bits, without any thread safety check features. | @Unity.Collections.LowLevel.Unsafe.UnsafeBitArray
`NativeHashMap`         | Unordered associative array, a collection of keys and values. | @Unity.Collections.NativeHashMap-2
`UnsafeHashMap`         | Unordered associative array, a collection of keys and values, without any thread safety check features. | @Unity.Collections.LowLevel.Unsafe.UnsafeHashMap`2
`NativeHashSet`         | Set of values. | @Unity.Collections.NativeHashSet`1
`UnsafeHashSet`         | Set of values, without any thread safety check features. | @Unity.Collections.LowLevel.Unsafe.UnsafeHashSet`1
`NativeList`            | An unmanaged, resizable list. | @Unity.Collections.NativeList`1
`UnsafeList`            | An unmanaged, resizable list, without any thread safety check features. | @Unity.Collections.LowLevel.Unsafe.UnsafeList`1
`NativeMultiHashMap`    | Unordered associative array, a collection of keys and values. This container can store multiple values for every key. | @Unity.Collections.NativeMultiHashMap`2
`UnsafeMultiHashMap`    | Unordered associative array, a collection of keys and values, without any thread safety check features. This container can store multiple values for every key. | @Unity.Collections.LowLevel.Unsafe.UnsafeMultiHashMap`2
`NativeStream`          | A deterministic data streaming supporting parallel reading and parallel writing. Allows you to write different types or arrays into a single stream. | @Unity.Collections.NativeStream
`UnsafeStream`          | A deterministic data streaming supporting parallel reading and parallel writings, without any thread safety check features. Allows you to write different types or arrays into a single stream. | @Unity.Collections.LowLevel.Unsafe.UnsafeStream
`NativeReference`       | An unmanaged, reference container. | @Unity.Collections.NativeReference`1
`UnsafeAppendBuffer`    | An unmanaged, untyped, buffer, without any thread safety check features. | @Unity.Collections.LowLevel.Unsafe.UnsafeAppendBuffer
`UnsafeRingQueue`       | Fixed-size circular buffer, without any thread safety check features. | @Unity.Collections.LowLevel.Unsafe.UnsafeRingQueue`1
`UnsafeAtomicCounter32` | 32-bit atomic counter. | @Unity.Collections.LowLevel.Unsafe.UnsafeAtomicCounter32
`UnsafeAtomicCounter64` | 64-bit atomic counter. | @Unity.Collections.LowLevel.Unsafe.UnsafeAtomicCounter64
[...](https://docs.unity3d.com/Packages/com.unity.collections@0.12/manual/index

The items in this package build upon the [NativeArray<T0>](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeArray_1),
[NativeSlice<T0>](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeSlice_1),
and other members of the Unity.Collections namespace, which Unity includes in
the [core module](https://docs.unity3d.com/ScriptReference/UnityEngine.CoreModule).

## Notation

`Native*` container prefix signifies that containers have debug safety mechanisms
which will warn users when a container is used incorrectly in regard with thread-safety,
or memory management. `Unsafe*` containers do not provide those safety warnings, and
the user is fully responsible to guarantee that code will execute correctly. Almost all
`Native*` containers are implemented by using `Unsafe*` container of the same kind
internally. In the release build, since debug safety mechanism is disabled, there
should not be any significant performance difference between `Unsafe*` and `Native*`
containers. `Unsafe*` containers are in `Unity.Collections.LowLevel.Unsafe`
namespace, while `Native*` containers are in `Unity.Collections` namespace.

## Determinism

Populating containers from parallel jobs is never deterministic, except when
using `NativeStream` or `UnsafeStream`. If determinism is required, consider
sorting the container as a separate step or post-process it on a single thread.

## Known Issues

All containers allocated with `Allocator.Temp` on the same thread use a shared
`AtomicSafetyHandle` instance. This is problematic when using `NativeHashMap`,
`NativeMultiHashMap`, `NativeHashSet` and `NativeList` together in situations
where their secondary safety handle is used. This means that operations that
invalidate an enumerator for either of these collections (or the `NativeArray`
returned by `NativeList.AsArray`) will also invalidate all other previously
acquired enumerators. For example, this will throw when safety checks are enabled:

```
var list = new NativeList<int>(Allocator.Temp);
list.Add(1);

// This array uses the secondary safety handle of the list, which is
// shared between all Allocator.Temp allocations.
var array = list.AsArray();

var list2 = new NativeHashSet<int>(Allocator.Temp);

// This invalidates the secondary safety handle, which is also used
// by the list above.
list2.TryAdd(1);

// This throws an InvalidOperationException because the shared safety
// handle was invalidated.
var x = array[0];
```
This defect will be addressed in a future release.
