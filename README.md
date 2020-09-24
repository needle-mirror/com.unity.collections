# Unity.Collections

A C# collections library providing data structures that can be used in jobs, and
optimized by Burst compiler.

## Documentation

https://docs.unity3d.com/Packages/com.unity.collections@0.12/manual/index.html

## Data structures

The Unity.Collections package includes the following data structures:

Data structure          | Description | Documentation
----------------------- | ----------- | -------------
`BitField32`            | Fixed size 32-bit array of bits. | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.BitField32.html)
`BitField64`            | Fixed size 64-bit array of bits. | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.BitField64.html)
`NativeBitArray`        | Arbitrary sized array of bits.   | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.NativeBitArray.html)
`UnsafeBitArray`        | Arbitrary sized array of bits, without any thread safety check features. | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.LowLevel.Unsafe.UnsafeBitArray.html)
`NativeHashMap`         | Unordered associative array, a collection of keys and values. | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.NativeHashMap-2.html)
`UnsafeHashMap`         | Unordered associative array, a collection of keys and values, without any thread safety check features. | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.LowLevel.Unsafe.UnsafeHashMap-2.html)
`NativeHashSet`         | Set of values. | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.NativeHashSet-1.html)
`UnsafeHashSet`         | Set of values, without any thread safety check features. | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.LowLevel.Unsafe.UnsafeHashSet-1.html)
`NativeList`            | An unmanaged, resizable list. | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.NativeList-1.html)
`UnsafeList`            | An unmanaged, resizable list, without any thread safety check features. | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.LowLevel.Unsafe.UnsafeList-1.html)
`NativeMultiHashMap`    | Unordered associative array, a collection of keys and values. This container can store multiple values for every key. | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.NativeMultiHashMap-2.html)
`UnsafeMultiHashMap`    | Unordered associative array, a collection of keys and values, without any thread safety check features. This container can store multiple values for every key. | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.LowLevel.Unsafe.UnsafeMultiHashMap-2.html)
`NativeStream`          | A deterministic data streaming supporting parallel reading and parallel writing. Allows you to write different types or arrays into a single stream. | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.NativeStream.html)
`UnsafeStream`          | A deterministic data streaming supporting parallel reading and parallel writings, without any thread safety check features. Allows you to write different types or arrays into a single stream. | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.LowLevel.Unsafe.UnsafeStream.html)
`NativeReference`       | An unmanaged, reference container. | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.NativeReference-1.html)
`UnsafeAppendBuffer`    | An unmanaged, untyped, buffer, without any thread safety check features. | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.LowLevel.Unsafe.UnsafeAppendBuffer.html)
`UnsafeRingQueue`       | Fixed-size circular buffer, without any thread safety check features. | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.LowLevel.Unsafe.UnsafeRingQueue-1.html)
`UnsafeAtomicCounter32` | 32-bit atomic counter. | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.LowLevel.Unsafe.UnsafeAtomicCounter32.html)
`UnsafeAtomicCounter64` | 64-bit atomic counter. | [Documentation](https://docs.unity3d.com/Packages/com.unity.collections@0.12/api/Unity.Collections.LowLevel.Unsafe.UnsafeAtomicCounter64.html)
[...](https://docs.unity3d.com/Packages/com.unity.collections@0.12/manual/index.html)

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

## Licensing

Unity Companion License (“License”) Software Copyright © 2017-2020 Unity Technologies ApS

For licensing details see [LICENSE.md](LICENSE.md)
