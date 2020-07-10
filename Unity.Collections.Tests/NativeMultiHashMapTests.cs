using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.Tests;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;

internal class NativeMultiHashMapTests : CollectionsTestFixture
{
    // These tests require:
    // - JobsDebugger support for static safety IDs (added in 2020.1)
    // - Asserting throws
#if !UNITY_DOTSRUNTIME
    [Test, DotsRuntimeIgnore]
    public void NativeMultiHashMap_UseAfterFree_UsesCustomOwnerTypeName()
    {
        var container = new NativeMultiHashMap<int, int>(10, Allocator.TempJob);
        container.Add(0, 123);
        container.Dispose();
        Assert.That(() => container.ContainsKey(0),
#if UNITY_2020_2_OR_NEWER
            Throws.Exception.TypeOf<ObjectDisposedException>()
#else
            Throws.InvalidOperationException
#endif
                .With.Message.Contains($"The {container.GetType()} has been deallocated"));
    }

    [BurstCompile(CompileSynchronously = true)]
    struct NativeMultiHashMap_CreateAndUseAfterFreeBurst : IJob
    {
        public void Execute()
        {
            var container = new NativeMultiHashMap<int, int>(10, Allocator.Temp);
            container.Add(0, 17);
            container.Dispose();
            container.Add(1, 42);
        }
    }

    [Test, DotsRuntimeIgnore]
    public void NativeMultiHashMap_CreateAndUseAfterFreeInBurstJob_UsesCustomOwnerTypeName()
    {
        // Make sure this isn't the first container of this type ever created, so that valid static safety data exists
        var container = new NativeMultiHashMap<int, int>(10, Allocator.TempJob);
        container.Dispose();

        var job = new NativeMultiHashMap_CreateAndUseAfterFreeBurst
        {
        };

        // Two things:
        // 1. This exception is logged, not thrown; thus, we use LogAssert to detect it.
        // 2. Calling write operation after container.Dispose() emits an unintuitive error message. For now, all this test cares about is whether it contains the
        //    expected type name.
        job.Run();
        LogAssert.Expect(LogType.Exception,
            new Regex($"InvalidOperationException: The {Regex.Escape(container.GetType().ToString())} has been declared as \\[ReadOnly\\] in the job, but you are writing to it"));
    }
#endif
}
