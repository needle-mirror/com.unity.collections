using NUnit.Framework;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.Tests;
using Unity.Jobs;

class NativeReferenceTests : CollectionsTestCommonBase
{
    [Test]
    public void NativeReference_AllocateDeallocate_ReadWrite()
    {
        var reference = new NativeReference<int>(Allocator.Persistent);
        reference.Value = 1;

        Assert.That(reference.Value, Is.EqualTo(1));

        reference.Dispose();
    }

    [Test]
    public void NativeReference_CopyFrom()
    {
        var referenceA = new NativeReference<TestData>(Allocator.Persistent);
        var referenceB = new NativeReference<TestData>(Allocator.Persistent);

        referenceA.Value = new TestData { Integer = 42, Float = 3.1416f };
        referenceB.CopyFrom(referenceA);

        Assert.That(referenceB.Value, Is.EqualTo(referenceA.Value));

        referenceA.Dispose();
        referenceB.Dispose();
    }

    [Test]
    public void NativeReference_CopyTo()
    {
        var referenceA = new NativeReference<TestData>(Allocator.Persistent);
        var referenceB = new NativeReference<TestData>(Allocator.Persistent);

        referenceA.Value = new TestData { Integer = 42, Float = 3.1416f };
        referenceA.CopyTo(referenceB);

        Assert.That(referenceB.Value, Is.EqualTo(referenceA.Value));

        referenceA.Dispose();
        referenceB.Dispose();
    }

    [Test]
    public void NativeReference_NullThrows()
    {
        var reference = new NativeReference<int>();
#if UNITY_DOTSRUNTIME    // The runtime safety system isn't quite the same, and will return InvalidOperationException in this case.
        Assert.Throws<InvalidOperationException>(() => reference.Value = 5);
#else
        Assert.Throws<NullReferenceException>(() => reference.Value = 5);
#endif
    }

    [Test]
    public void NativeReference_CopiedIsKeptInSync()
    {
        var reference = new NativeReference<int>(Allocator.Persistent);
        var referenceCopy = reference;
        reference.Value = 42;

        Assert.That(reference.Value, Is.EqualTo(referenceCopy.Value));

        reference.Dispose();
    }

    struct TestData
    {
        public int Integer;
        public float Float;
    }

    [BurstCompile(CompileSynchronously = true)]
    struct TempNativeReferenceInJob : IJob
    {
        public NativeReference<int> Output;

        public void Execute()
        {
            var reference = new NativeReference<int>(Allocator.Temp);
            reference.Value = 42;
            Output.Value = reference.Value;
            reference.Dispose();
        }
    }

    [Test]
    public void NativeReference_TempInBurstJob()
    {
        var job = new TempNativeReferenceInJob() { Output = new NativeReference<int>(Allocator.TempJob) };
        job.Schedule().Complete();

        Assert.That(job.Output.Value, Is.EqualTo(42));

        job.Output.Dispose();
    }

    [Test]
    public void NativeReference_DisposeJob()
    {
        var reference = new NativeReference<int>(Allocator.Persistent);
        Assert.That(reference.IsCreated, Is.True);
        Assert.DoesNotThrow(() => reference.Value = 99);

        var disposeJob = reference.Dispose(default);
        Assert.That(reference.IsCreated, Is.False);

#if UNITY_2020_2_OR_NEWER
        Assert.Throws<ObjectDisposedException>(
#else
        Assert.Throws<InvalidOperationException>(
#endif
            () => reference.Value = 3);


        disposeJob.Complete();
    }

    [Test]
    public void NativeReference_NoGCAllocations()
    {
        var reference = new NativeReference<int>(Allocator.Persistent);

        GCAllocRecorder.ValidateNoGCAllocs(() =>
        {
            reference.Value = 1;
            reference.Value++;
        });

        Assert.That(reference.Value, Is.EqualTo(2));

        reference.Dispose();
    }

    [Test]
    public void NativeReference_Equals()
    {
        var referenceA = new NativeReference<int>(12345, Allocator.Persistent);
        var referenceB = new NativeReference<int>(Allocator.Persistent) { Value = 12345 };
        Assert.That(referenceA, Is.EqualTo(referenceB));

        referenceB.Value = 54321;
        Assert.AreNotEqual(referenceA, referenceB);

        referenceA.Dispose();
        referenceB.Dispose();
    }

    [Test]
    public void NativeReference_GetHashCode()
    {
        var integer = 42;
        var reference = new NativeReference<int>(integer, Allocator.Persistent);
        Assert.That(reference.GetHashCode(), Is.EqualTo(integer.GetHashCode()));

        reference.Dispose();
    }
}
