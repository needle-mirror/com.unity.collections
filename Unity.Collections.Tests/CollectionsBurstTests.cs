#if UNITY_EDITOR && !UNITY_2020_2_OR_NEWER
// disable on 2020.2 until DOTS-2592 is resolved
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.Tests;

[TestFixture, EmbeddedPackageOnlyTest]
public class CollectionsBurstTests : BurstCompatibilityTests
{
    public CollectionsBurstTests()
        : base("Packages/com.unity.collections/Unity.Collections.Tests/_generated_burst_tests.cs",
            "Unity.Collections")
    {
    }
}

/// <summary>
/// Tests structs that live in Unity.Collections.Tests. For testing the [BurstCompatible] attribute.
/// </summary>
[TestFixture, EmbeddedPackageOnlyTest]
public class BurstCompatibilityUnitTests : BurstCompatibilityTests
{
    public BurstCompatibilityUnitTests()
        : base("Packages/com.unity.collections/Unity.Collections.Tests/_internal_generated_burst_tests.cs",
            "Unity.Collections.Tests")
    {
    }
}

namespace Unity.Collections.Tests
{
    [BurstCompatible]
    unsafe struct BurstCompatibleIndexerTest
    {
        double* ptr;

        public BurstCompatibleIndexerTest(double* p)
        {
            ptr = p;
        }

        public double this[int index]
        {
            get => ptr[index];
            set => ptr[index] = value;
        }
    }

    [BurstCompatible]
    unsafe struct BurstCompatibleMultiDimensionalIndexerTest
    {
        double* ptr;

        public BurstCompatibleMultiDimensionalIndexerTest(double* p)
        {
            ptr = p;
        }

        public double this[ulong index1, uint index2]
        {
            get => ptr[index1 + index2];
            set => ptr[index1 + index2] = value;
        }
    }

    // To verify this case https://unity3d.atlassian.net/browse/DOTS-3165
    [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
    struct BurstCompatibleUseSameGenericTypeWithDifferentStruct1<T> where T : struct
    {
        public T Value;

        public BurstCompatibleUseSameGenericTypeWithDifferentStruct1(T value)
        {
            Value = value;
        }

        public unsafe int CompareTo(BurstCompatibleUseSameGenericTypeWithDifferentStruct2<T> other)
        {
            return UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref other.Value), UnsafeUtility.AddressOf(ref Value), UnsafeUtility.SizeOf<T>());
        }
    }

    // To verify this case https://unity3d.atlassian.net/browse/DOTS-3165
    [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
    struct BurstCompatibleUseSameGenericTypeWithDifferentStruct2<T> where T : struct
    {
        public T Value;

        public BurstCompatibleUseSameGenericTypeWithDifferentStruct2(T value)
        {
            Value = value;
        }

        public unsafe int CompareTo(BurstCompatibleUseSameGenericTypeWithDifferentStruct1<T> other)
        {
            return UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref other.Value), UnsafeUtility.AddressOf(ref Value), UnsafeUtility.SizeOf<T>());
        }
    }
}
#endif
