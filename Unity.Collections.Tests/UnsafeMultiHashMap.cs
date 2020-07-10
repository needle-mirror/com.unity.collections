using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

internal class UnsafeMultiHashMapTests
{
    [BurstCompile(CompileSynchronously = true)]
    public struct UnsafeMultiHashMapAddJob : IJobParallelFor
    {
        public UnsafeMultiHashMap<int, int>.ParallelWriter Writer;

        public void Execute(int index)
        {
            Writer.Add(123, index);
        }
    }

    [Test]
    public void UnsafeMultiHashMap_AddJob()
    {
        var container = new UnsafeMultiHashMap<int, int>(32, Allocator.TempJob);

        var job = new UnsafeMultiHashMapAddJob()
        {
            Writer = container.AsParallelWriter(),
        };

        job.Schedule(3, 1).Complete();

        Assert.True(container.ContainsKey(123));
        Assert.AreEqual(container.CountValuesForKey(123), 3);

        container.Dispose();
    }

    [Test]
    public void UnsafeHashMap_RemoveOnEmptyMap_DoesNotThrow()
    {
        var container = new UnsafeHashMap<int, int>(0, Allocator.Temp);
        Assert.DoesNotThrow(() => container.Remove(0));
        Assert.DoesNotThrow(() => container.Remove(-425196));
        container.Dispose();
    }

    [Test]
    public void UnsafeMultiHashMap_RemoveOnEmptyMap_DoesNotThrow()
    {
        var container = new UnsafeMultiHashMap<int, int>(0, Allocator.Temp);

        Assert.DoesNotThrow(() => container.Remove(0));
        Assert.DoesNotThrow(() => container.Remove(-425196));
        Assert.DoesNotThrow(() => container.Remove(0, 0));
        Assert.DoesNotThrow(() => container.Remove(-425196, 0));

        container.Dispose();
    }

    [Test]
    public void UnsafeMultiHashMap_ForEach()
    {
        using (var container = new UnsafeMultiHashMap<int, int>(1, Allocator.Temp))
        {
            for (int i = 0; i < 30; ++i)
            {
                container.Add(i, 30 + i);
                container.Add(i, 60 + i);
            }

            var count = 0;
            foreach (var kv in container)
            {
                if (kv.Value < 60)
                {
                    Assert.AreEqual(kv.Key + 30, kv.Value);
                }
                else
                {
                    Assert.AreEqual(kv.Key + 60, kv.Value);
                }

                ++count;
            }

            Assert.AreEqual(container.Count(), count);
        }
    }
}
