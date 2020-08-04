using System;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.TestTools;
#if !UNITY_PORTABLE_TEST_RUNNER
using System.Text.RegularExpressions;
#endif

internal class UnsafeHashSetTests
{
    static void ExpectedCount<TT>(ref UnsafeHashSet<TT> container, int expected)
        where TT : unmanaged, IEquatable<TT>
    {
        Assert.AreEqual(expected == 0, container.IsEmpty);
        Assert.AreEqual(expected, container.Count());
    }

    [Test]
    public void UnsafeHashSet_IsEmpty()
    {
        var container = new UnsafeHashSet<int>(0, Allocator.Persistent);
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
    public void UnsafeHashSet_Full_Throws()
    {
        var container = new UnsafeHashSet<int>(16, Allocator.Temp);
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
    public void UnsafeHashSet_RemoveOnEmptyMap_DoesNotThrow()
    {
        var container = new UnsafeHashSet<int>(0, Allocator.Temp);
        Assert.DoesNotThrow(() => container.Remove(0));
        Assert.DoesNotThrow(() => container.Remove(-425196));
        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_Collisions()
    {
        var container = new UnsafeHashSet<int>(16, Allocator.Temp);

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
    public void UnsafeHashSet_SameElement()
    {
        using (var container = new UnsafeHashSet<int>(0, Allocator.Persistent))
        {
            Assert.IsTrue(container.Add(0));
            Assert.IsFalse(container.Add(0));
        }
    }

    [Test]
    public void UnsafeHashSet_ForEach_FixedStringInHashMap()
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
    public void UnsafeHashSet_ForEach([Values(10, 1000)]int n)
    {
        var seen = new NativeArray<int>(n, Allocator.Temp);
        using (var container = new UnsafeHashSet<int>(32, Allocator.TempJob))
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
}
