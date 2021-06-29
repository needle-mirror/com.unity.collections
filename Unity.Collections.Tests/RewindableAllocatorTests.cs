using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.Tests;

#if !UNITY_DOTSRUNTIME && ENABLE_UNITY_COLLECTIONS_CHECKS
internal class RewindableAllocatorTests : CollectionsTestFixture
{

    public void RewindInvalidatesNativeList()
    {
        RewindableAllocator allocator = default;
        allocator.Initialize(1024 * 1024);
        var container = allocator.AllocateNativeList<byte>(allocator.InitialSizeInBytes / 1000);
        container.Resize(1, NativeArrayOptions.ClearMemory);
        container[0] = 0xFE;
        allocator.Rewind();
        Assert.Throws<ObjectDisposedException>(() =>
        {
            container[0] = 0xEF;
        });
        allocator.Dispose();
    }

    [Test]
    public void RewindInvalidatesNativeArray()
    {
        RewindableAllocator allocator = default;
        allocator.Initialize(1024 * 1024);
        var container = allocator.AllocateNativeArray<byte>(allocator.InitialSizeInBytes / 1000);
        container[0] = 0xFE;
        allocator.Rewind();
        Assert.Throws<ObjectDisposedException>(() =>
        {
            container[0] = 0xEF;
        });
        allocator.Dispose();
    }

    [Test]
    public void NativeListCanBeCreatedViaMemberFunction()
    {
        RewindableAllocator allocator = default;
        allocator.Initialize(1024 * 1024);
        var container = allocator.AllocateNativeList<byte>(allocator.InitialSizeInBytes / 1000);
        container.Resize(1, NativeArrayOptions.ClearMemory);
        container[0] = 0xFE;
        allocator.Dispose();
    }

    [Test]
    public void NativeListCanBeDisposed()
    {
        RewindableAllocator allocator = default;
        allocator.Initialize(1024 * 1024);
        var container = allocator.AllocateNativeList<byte>(allocator.InitialSizeInBytes / 1000);
        container.Resize(1, NativeArrayOptions.ClearMemory);
        container[0] = 0xFE;
        container.Dispose();
        allocator.Rewind();
        allocator.Dispose();
    }

    [Test]
    public void NativeArrayCanBeDisposed()
    {
        RewindableAllocator allocator = default;
        allocator.Initialize(1024 * 1024);
        var container = allocator.AllocateNativeArray<byte>(allocator.InitialSizeInBytes / 1000);
        container[0] = 0xFE;
        container.Dispose();
        allocator.Rewind();
        allocator.Dispose();
    }

    [Test]
    public void NumberOfBlocksIsTemporarilyStable()
    {
        RewindableAllocator allocator = default;
        allocator.Initialize(1024 * 1024);
        allocator.AllocateNativeList<byte>(allocator.InitialSizeInBytes * 10);
        var blocksBefore = allocator.BlocksAllocated;
        allocator.Rewind();
        var blocksAfter = allocator.BlocksAllocated;
        Assert.AreEqual(blocksAfter, blocksBefore);
        allocator.Dispose();
    }

    [Test]
    public void NumberOfBlocksEventuallyDrops()
    {
        RewindableAllocator allocator = default;
        allocator.Initialize(1024 * 1024);
        allocator.AllocateNativeList<byte>(allocator.InitialSizeInBytes * 10);
        var blocksBefore = allocator.BlocksAllocated;
        allocator.Rewind();
        allocator.Rewind();
        var blocksAfter = allocator.BlocksAllocated;
        Assert.IsTrue(blocksAfter < blocksBefore);
        allocator.Dispose();
    }

    [Test]
    public void PossibleToAllocateGigabytes()
    {
        RewindableAllocator allocator = default;
        allocator.Initialize(1024 * 1024);
        const int giga = 1024 * 1024 * 1024;
        var container0 = allocator.AllocateNativeList<byte>(giga);
        var container1 = allocator.AllocateNativeList<byte>(giga);
        var container2 = allocator.AllocateNativeList<byte>(giga);
        container0.Resize(1, NativeArrayOptions.ClearMemory);
        container1.Resize(1, NativeArrayOptions.ClearMemory);
        container2.Resize(1, NativeArrayOptions.ClearMemory);
        container0[0] = 0;
        container1[0] = 1;
        container2[0] = 2;
        Assert.AreEqual((byte)0, container0[0]);
        Assert.AreEqual((byte)1, container1[0]);
        Assert.AreEqual((byte)2, container2[0]);
        allocator.Dispose();
    }

    [Test]
    public void ExhaustsFirstBlockBeforeAllocatingMore()
    {
        RewindableAllocator allocator = default;
        allocator.Initialize(1024 * 1024);
        for (var i = 0; i < 50; ++i)
        {
            allocator.AllocateNativeList<byte>(allocator.InitialSizeInBytes / 100);
            Assert.AreEqual(1, allocator.BlocksAllocated);
        }
        allocator.AllocateNativeList<byte>(allocator.InitialSizeInBytes);
        Assert.AreEqual(2, allocator.BlocksAllocated);
        allocator.Dispose();
    }
}

#endif
