using System;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.Tests;
using UnityEngine;
using UnityEngine.TestTools;
#if !UNITY_PORTABLE_TEST_RUNNER
using System.Text.RegularExpressions;
#endif

internal class NativeHashSetTests: CollectionsTestFixture
{
    static void ExpectedCount<T>(ref NativeHashSet<T> container, int expected)
        where T : unmanaged, IEquatable<T>
    {
        Assert.AreEqual(expected == 0, container.IsEmpty);
        Assert.AreEqual(expected, container.Count());
    }

    [Test]
    public void NativeHashSet_IsEmpty()
    {
        var container = new NativeHashSet<int>(0, Allocator.Persistent);
        Assert.IsTrue(container.IsEmpty);

        Assert.IsTrue(container.Add(0));
        Assert.IsFalse(container.IsEmpty);
        Assert.AreEqual(1, container.Capacity);
        ExpectedCount(ref container, 1);

        container.Remove(0);
        Assert.IsTrue(container.IsEmpty);

        Assert.IsTrue(container.Add(0));
        container.Clear();
        Assert.IsTrue(container.IsEmpty);

        container.Dispose();
    }

#if !UNITY_DOTSRUNTIME    // DOTS-Runtime has an assertion in the C++ layer, that can't be caught in C#
    [Test]
    public void NativeHashSet_Full_Throws()
    {
        var container = new NativeHashSet<int>(16, Allocator.Temp);
        ExpectedCount(ref container, 0);

        for (int i = 0, capacity = container.Capacity; i < capacity; ++i)
        {
            Assert.DoesNotThrow(() => { container.Add(i); });
        }
        ExpectedCount(ref container, container.Capacity);

        // Make sure overallocating throws and exception if using the Concurrent version - normal hash map would grow
        var writer = container.AsParallelWriter();
        Assert.Throws<System.InvalidOperationException>(() => { writer.Add(100); });
        ExpectedCount(ref container, container.Capacity);

        container.Clear();
        ExpectedCount(ref container, 0);

        container.Dispose();
    }
#endif

    [Test]
    public void NativeHashSet_RemoveOnEmptyMap_DoesNotThrow()
    {
        var container = new NativeHashSet<int>(0, Allocator.Temp);
        Assert.DoesNotThrow(() => container.Remove(0));
        Assert.DoesNotThrow(() => container.Remove(-425196));
        container.Dispose();
    }

    [Test]
    public void NativeHashSet_Double_Deallocate_Throws()
    {
        var hashMap = new NativeHashSet<int>(16, Allocator.TempJob);
        hashMap.Dispose();
#if UNITY_2020_2_OR_NEWER
        Assert.Throws<ObjectDisposedException>(
#else
        Assert.Throws<InvalidOperationException>(
#endif
            () => { hashMap.Dispose(); });
    }

    [Test]
    public void NativeHashSet_Collisions()
    {
        var container = new NativeHashSet<int>(16, Allocator.Temp);

        Assert.IsFalse(container.Contains(0), "Contains on empty hash map did not fail");
        ExpectedCount(ref container, 0);

        // Make sure inserting values work
        for (int i = 0; i < 8; ++i)
        {
            Assert.IsTrue(container.Add(i), "Failed to add value");
        }
        ExpectedCount(ref container, 8);

        // The bucket size is capacity * 2, adding that number should result in hash collisions
        for (int i = 0; i < 8; ++i)
        {
            Assert.IsTrue(container.Add(i + 32), "Failed to add value with potential hash collision");
        }

        // Make sure reading the inserted values work
        for (int i = 0; i < 8; ++i)
        {
            Assert.IsTrue(container.Contains(i), "Failed get value from hash set");
        }

        for (int i = 0; i < 8; ++i)
        {
            Assert.IsTrue(container.Contains(i + 32), "Failed get value from hash set");
        }

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_SameElement()
    {
        using (var container = new NativeHashSet<int>(0, Allocator.Persistent))
        {
            Assert.IsTrue(container.Add(0));
            Assert.IsFalse(container.Add(0));
        }
    }

    [Test]
    public void NativeHashSet_ParallelWriter_CanBeUsedInJob()
    {
        const int count = 32;
        using (var hashSet = new NativeHashSet<int>(count, Allocator.TempJob))
        {
            new ParallelWriteToHashSetJob
            {
                Writer = hashSet.AsParallelWriter()
            }.Schedule(count, 2).Complete();

            var result = hashSet.ToNativeArray(Allocator.Temp);
            result.Sort();
            for (int i = 0; i < count; i++)
                Assert.AreEqual(i, result[i]);
        }
    }

    struct ParallelWriteToHashSetJob : IJobParallelFor
    {
        [WriteOnly]
        public NativeHashSet<int>.ParallelWriter Writer;

        public void Execute(int index)
        {
            Writer.Add(index);
        }
    }

    [Test]
    public void NativeHashSet_CanBeReadFromJob()
    {
        using (var hashSet = new NativeHashSet<int>(1, Allocator.TempJob))
        using (var result = new NativeReference<int>(Allocator.TempJob))
        {
            hashSet.Add(42);
            new ReadHashSetJob
            {
                Input = hashSet,
                Output = result,
            }.Run();
            Assert.AreEqual(42, result.Value);
        }
    }

    struct ReadHashSetJob : IJob
    {
        [ReadOnly]
        public NativeHashSet<int> Input;

        public NativeReference<int> Output;
        public void Execute()
        {
            Output.Value = Input.ToNativeArray(Allocator.Temp)[0];
        }
    }

    [Test]
    public void NativeHashSet_ForEach_FixedStringInHashMap()
    {
        using (var stringList = new NativeList<FixedString32>(10, Allocator.Persistent) { "Hello", ",", "World", "!" })
        {
            var container = new NativeHashSet<FixedString128>(50, Allocator.Temp);
            var seen = new NativeArray<int>(stringList.Length, Allocator.Temp);
            foreach (var str in stringList)
            {
                container.Add(str);
            }

            foreach (var value in container)
            {
                int index = stringList.IndexOf(value);
                Assert.AreEqual(stringList[index], value.ToString());
                seen[index] = seen[index] + 1;
            }

            for (int i = 0; i < stringList.Length; i++)
            {
                Assert.AreEqual(1, seen[i], $"Incorrect value count {stringList[i]}");
            }
        }
    }

    [Test]
    public void NativeHashSet_ForEach([Values(10, 1000)]int n)
    {
        var seen = new NativeArray<int>(n, Allocator.Temp);
        using (var container = new NativeHashSet<int>(32, Allocator.TempJob))
        {
            for (int i = 0; i < n; i++)
            {
                container.Add(i);
            }

            var count = 0;
            foreach (var item in container)
            {
                Assert.True(container.Contains(item));
                seen[item] = seen[item] + 1;
                ++count;
            }

            Assert.AreEqual(container.Count(), count);
            for (int i = 0; i < n; i++)
            {
                Assert.AreEqual(1, seen[i], $"Incorrect item count {i}");
            }
        }
    }

    [Test]
    public void NativeHashSet_ForEach_Throws_When_Modified()
    {
        using (var container = new NativeHashSet<int>(32, Allocator.TempJob))
        {
            container.Add(0);
            container.Add(1);
            container.Add(2);
            container.Add(3);
            container.Add(4);
            container.Add(5);
            container.Add(6);
            container.Add(7);
            container.Add(8);
            container.Add(9);

#if UNITY_2020_2_OR_NEWER
            Assert.Throws<ObjectDisposedException>(() =>
#else
            Assert.Throws<InvalidOperationException>(() =>
#endif
            {
                foreach (var item in container)
                {
                    container.Add(10);
                }
            });

#if UNITY_2020_2_OR_NEWER
            Assert.Throws<ObjectDisposedException>(() =>
#else
            Assert.Throws<InvalidOperationException>(() =>
#endif
            {
                foreach (var item in container)
                {
                    container.Remove(1);
                }
            });
        }
    }

    [Test]
    public void NativeHashSet_ForEach_Throws()
    {
        using (var container = new NativeHashSet<int>(32, Allocator.TempJob))
        {
            var iter = container.GetEnumerator();

            var jobHandle = new ParallelWriteToHashSetJob
            {
                Writer = container.AsParallelWriter()

            }.Schedule(1, 2);

#if UNITY_2020_2_OR_NEWER
            Assert.Throws<ObjectDisposedException>(() =>
#else
            Assert.Throws<InvalidOperationException>(() =>
#endif
            {
                while (iter.MoveNext())
                {
                }
            });

            jobHandle.Complete();
        }
    }

    struct ForEachIterator : IJob
    {
        [ReadOnly]
        public NativeHashSet<int>.Enumerator Iter;

        public void Execute()
        {
            while (Iter.MoveNext())
            {
            }
        }
    }

    [Test]
    public void NativeHashSet_ForEach_Throws_Job_Iterator()
    {
        using (var container = new NativeHashSet<int>(32, Allocator.TempJob))
        {
            var jobHandle = new ForEachIterator
            {
                Iter = container.GetEnumerator()

            }.Schedule();

            Assert.Throws<InvalidOperationException>(() => { container.Add(1); });

            jobHandle.Complete();
        }
    }
}
