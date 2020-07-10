using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

internal class UnsafeHashMapTests
{
    // Burst error BC1071: Unsupported assert type
    // [BurstCompile(CompileSynchronously = true)]
    public struct UnsafeHashMapAddJob : IJob
    {
        public UnsafeHashMap<int, int>.ParallelWriter Writer;

        public void Execute()
        {
            Assert.True(Writer.TryAdd(123, 1));
        }
    }

    [Test]
    public void UnsafeHashMap_AddJob()
    {
        var container = new UnsafeHashMap<int, int>(32, Allocator.TempJob);

        var job = new UnsafeHashMapAddJob()
        {
            Writer = container.AsParallelWriter(),
        };

        job.Schedule().Complete();

        Assert.True(container.ContainsKey(123));

        container.Dispose();
    }

    [Test]
    public void UnsafeHashMap_ForEach()
    {
        using (var container = new UnsafeHashMap<int, int>(32, Allocator.TempJob))
        {
            container.Add(0, 012);
            container.Add(1, 123);
            container.Add(2, 234);
            container.Add(3, 345);
            container.Add(4, 456);
            container.Add(5, 567);
            container.Add(6, 678);
            container.Add(7, 789);
            container.Add(8, 890);
            container.Add(9, 901);

            var count = 0;
            foreach (var kv in container)
            {
                int value;
                Assert.True(container.TryGetValue(kv.Key, out value));
                Assert.AreEqual(value, kv.Value);

                ++count;
            }

            Assert.AreEqual(container.Count(), count);
        }
    }
}
