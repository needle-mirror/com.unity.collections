using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.Tests;
using Unity.Jobs;

internal class UnsafeStreamTests : CollectionsTestCommonBase
{
    [Test]
    public void UnsafeStream_CustomAllocatorTest()
    {
        AllocatorManager.Initialize();
        CustomAllocatorTests.CountingAllocator allocator = default;
        allocator.Initialize();

        using (var container = new UnsafeStream(1, allocator.Handle))
        {
        }

        Assert.IsTrue(allocator.WasUsed);
        allocator.Dispose();
        AllocatorManager.Shutdown();
    }

    [BurstCompile]
    struct BurstedCustomAllocatorJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public unsafe CustomAllocatorTests.CountingAllocator* Allocator;

        public void Execute()
        {
            unsafe
            {
                using (var container = new UnsafeStream(1, Allocator->Handle))
                {
                }
            }
        }
    }

    [Test]
    public void UnsafeStream_BurstedCustomAllocatorTest()
    {
        AllocatorManager.Initialize();
        CustomAllocatorTests.CountingAllocator allocator = default;
        allocator.Initialize();

        unsafe
        {
            var handle = new BurstedCustomAllocatorJob {Allocator = &allocator}.Schedule();
            handle.Complete();
        }

        Assert.IsTrue(allocator.WasUsed);
        allocator.Dispose();
        AllocatorManager.Shutdown();
    }
}
