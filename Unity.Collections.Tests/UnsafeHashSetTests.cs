using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.TestTools;

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

        container.Clear();
        Assert.IsTrue(container.IsEmpty);

        container.Dispose();
    }

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
    public void NativeHashSet_SameElement()
    {
        using (var container = new UnsafeHashSet<int>(0, Allocator.Persistent))
        {
            Assert.IsTrue(container.Add(0));
            Assert.IsFalse(container.Add(0));
        }
    }

    [Test]
    public void UnsafeHashSet_ForEach()
    {
        using (var container = new UnsafeHashSet<int>(32, Allocator.TempJob))
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

            var count = 0;
            foreach (var item in container)
            {
                Assert.True(container.Contains(item));

                ++count;
            }

            Assert.AreEqual(container.Count(), count);
        }
    }
}
