using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.Tests;

internal class NativeHashSetTestsGenerated : CollectionsTestFixture
{
    static void ExpectedCount<T>(ref NativeHashSet<T> container, int expected)
        where T : unmanaged, IEquatable<T>
    {
        Assert.AreEqual(expected == 0, container.IsEmpty);
        Assert.AreEqual(expected, container.Count());
    }


    [Test]
    public void NativeHashSet_NativeHashSet_EIU_ExceptWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new NativeHashSet<int>(8, Allocator.TempJob) { };
        container.ExceptWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_NativeHashSet_EIU_ExceptWith_AxB()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new NativeHashSet<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.ExceptWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_NativeHashSet_EIU_IntersectWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new NativeHashSet<int>(8, Allocator.TempJob) { };
        container.IntersectWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_NativeHashSet_EIU_IntersectWith()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new NativeHashSet<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.IntersectWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_NativeHashSet_EIU_UnionWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new NativeHashSet<int>(8, Allocator.TempJob) { };
        container.UnionWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_NativeHashSet_EIU_UnionWith()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new NativeHashSet<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.UnionWith(other);

        ExpectedCount(ref container, 9);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));
        Assert.True(container.Contains(6));
        Assert.True(container.Contains(7));
        Assert.True(container.Contains(8));

        container.Dispose();
        other.Dispose();
    }


    [Test]
    public void NativeHashSet_UnsafeHashSet_EIU_ExceptWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        container.ExceptWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_UnsafeHashSet_EIU_ExceptWith_AxB()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new UnsafeHashSet<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.ExceptWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_UnsafeHashSet_EIU_IntersectWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        container.IntersectWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_UnsafeHashSet_EIU_IntersectWith()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new UnsafeHashSet<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.IntersectWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_UnsafeHashSet_EIU_UnionWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        container.UnionWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_UnsafeHashSet_EIU_UnionWith()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new UnsafeHashSet<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.UnionWith(other);

        ExpectedCount(ref container, 9);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));
        Assert.True(container.Contains(6));
        Assert.True(container.Contains(7));
        Assert.True(container.Contains(8));

        container.Dispose();
        other.Dispose();
    }


    [Test]
    public void NativeHashSet_NativeList_EIU_ExceptWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new NativeList<int>(8, Allocator.TempJob) { };
        container.ExceptWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_NativeList_EIU_ExceptWith_AxB()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new NativeList<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.ExceptWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_NativeList_EIU_IntersectWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new NativeList<int>(8, Allocator.TempJob) { };
        container.IntersectWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_NativeList_EIU_IntersectWith()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new NativeList<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.IntersectWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_NativeList_EIU_UnionWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new NativeList<int>(8, Allocator.TempJob) { };
        container.UnionWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_NativeList_EIU_UnionWith()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new NativeList<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.UnionWith(other);

        ExpectedCount(ref container, 9);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));
        Assert.True(container.Contains(6));
        Assert.True(container.Contains(7));
        Assert.True(container.Contains(8));

        container.Dispose();
        other.Dispose();
    }


    [Test]
    public void NativeHashSet_UnsafeList_EIU_ExceptWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new UnsafeList<int>(8, Allocator.TempJob) { };
        container.ExceptWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_UnsafeList_EIU_ExceptWith_AxB()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new UnsafeList<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.ExceptWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_UnsafeList_EIU_IntersectWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new UnsafeList<int>(8, Allocator.TempJob) { };
        container.IntersectWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_UnsafeList_EIU_IntersectWith()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new UnsafeList<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.IntersectWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_UnsafeList_EIU_UnionWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new UnsafeList<int>(8, Allocator.TempJob) { };
        container.UnionWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void NativeHashSet_UnsafeList_EIU_UnionWith()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new UnsafeList<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.UnionWith(other);

        ExpectedCount(ref container, 9);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));
        Assert.True(container.Contains(6));
        Assert.True(container.Contains(7));
        Assert.True(container.Contains(8));

        container.Dispose();
        other.Dispose();
    }


    [Test]
    public void NativeHashSet_FixedList32_EIU_ExceptWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList32<int>() { };
        container.ExceptWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList32_EIU_ExceptWith_AxB()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList32<int>() { 3, 4, 5, 6, 7, 8 };
        container.ExceptWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList32_EIU_IntersectWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList32<int>() { };
        container.IntersectWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList32_EIU_IntersectWith()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList32<int>() { 3, 4, 5, 6, 7, 8 };
        container.IntersectWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList32_EIU_UnionWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList32<int>() { };
        container.UnionWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList32_EIU_UnionWith()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList32<int>() { 3, 4, 5, 6, 7, 8 };
        container.UnionWith(other);

        ExpectedCount(ref container, 9);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));
        Assert.True(container.Contains(6));
        Assert.True(container.Contains(7));
        Assert.True(container.Contains(8));

        container.Dispose();
    }
    [Test]
    public void NativeHashSet_FixedList64_EIU_ExceptWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList64<int>() { };
        container.ExceptWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList64_EIU_ExceptWith_AxB()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList64<int>() { 3, 4, 5, 6, 7, 8 };
        container.ExceptWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList64_EIU_IntersectWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList64<int>() { };
        container.IntersectWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList64_EIU_IntersectWith()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList64<int>() { 3, 4, 5, 6, 7, 8 };
        container.IntersectWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList64_EIU_UnionWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList64<int>() { };
        container.UnionWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList64_EIU_UnionWith()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList64<int>() { 3, 4, 5, 6, 7, 8 };
        container.UnionWith(other);

        ExpectedCount(ref container, 9);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));
        Assert.True(container.Contains(6));
        Assert.True(container.Contains(7));
        Assert.True(container.Contains(8));

        container.Dispose();
    }
    [Test]
    public void NativeHashSet_FixedList128_EIU_ExceptWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList128<int>() { };
        container.ExceptWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList128_EIU_ExceptWith_AxB()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList128<int>() { 3, 4, 5, 6, 7, 8 };
        container.ExceptWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList128_EIU_IntersectWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList128<int>() { };
        container.IntersectWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList128_EIU_IntersectWith()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList128<int>() { 3, 4, 5, 6, 7, 8 };
        container.IntersectWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList128_EIU_UnionWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList128<int>() { };
        container.UnionWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList128_EIU_UnionWith()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList128<int>() { 3, 4, 5, 6, 7, 8 };
        container.UnionWith(other);

        ExpectedCount(ref container, 9);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));
        Assert.True(container.Contains(6));
        Assert.True(container.Contains(7));
        Assert.True(container.Contains(8));

        container.Dispose();
    }
    [Test]
    public void NativeHashSet_FixedList512_EIU_ExceptWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList512<int>() { };
        container.ExceptWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList512_EIU_ExceptWith_AxB()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList512<int>() { 3, 4, 5, 6, 7, 8 };
        container.ExceptWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList512_EIU_IntersectWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList512<int>() { };
        container.IntersectWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList512_EIU_IntersectWith()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList512<int>() { 3, 4, 5, 6, 7, 8 };
        container.IntersectWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList512_EIU_UnionWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList512<int>() { };
        container.UnionWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList512_EIU_UnionWith()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList512<int>() { 3, 4, 5, 6, 7, 8 };
        container.UnionWith(other);

        ExpectedCount(ref container, 9);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));
        Assert.True(container.Contains(6));
        Assert.True(container.Contains(7));
        Assert.True(container.Contains(8));

        container.Dispose();
    }
    [Test]
    public void NativeHashSet_FixedList4096_EIU_ExceptWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList4096<int>() { };
        container.ExceptWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList4096_EIU_ExceptWith_AxB()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList4096<int>() { 3, 4, 5, 6, 7, 8 };
        container.ExceptWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList4096_EIU_IntersectWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList4096<int>() { };
        container.IntersectWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList4096_EIU_IntersectWith()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList4096<int>() { 3, 4, 5, 6, 7, 8 };
        container.IntersectWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList4096_EIU_UnionWith_Empty()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList4096<int>() { };
        container.UnionWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void NativeHashSet_FixedList4096_EIU_UnionWith()
    {
        var container = new NativeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList4096<int>() { 3, 4, 5, 6, 7, 8 };
        container.UnionWith(other);

        ExpectedCount(ref container, 9);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));
        Assert.True(container.Contains(6));
        Assert.True(container.Contains(7));
        Assert.True(container.Contains(8));

        container.Dispose();
    }
    static void ExpectedCount<T>(ref UnsafeHashSet<T> container, int expected)
        where T : unmanaged, IEquatable<T>
    {
        Assert.AreEqual(expected == 0, container.IsEmpty);
        Assert.AreEqual(expected, container.Count());
    }


    [Test]
    public void UnsafeHashSet_NativeHashSet_EIU_ExceptWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new NativeHashSet<int>(8, Allocator.TempJob) { };
        container.ExceptWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_NativeHashSet_EIU_ExceptWith_AxB()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new NativeHashSet<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.ExceptWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_NativeHashSet_EIU_IntersectWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new NativeHashSet<int>(8, Allocator.TempJob) { };
        container.IntersectWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_NativeHashSet_EIU_IntersectWith()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new NativeHashSet<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.IntersectWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_NativeHashSet_EIU_UnionWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new NativeHashSet<int>(8, Allocator.TempJob) { };
        container.UnionWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_NativeHashSet_EIU_UnionWith()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new NativeHashSet<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.UnionWith(other);

        ExpectedCount(ref container, 9);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));
        Assert.True(container.Contains(6));
        Assert.True(container.Contains(7));
        Assert.True(container.Contains(8));

        container.Dispose();
        other.Dispose();
    }


    [Test]
    public void UnsafeHashSet_UnsafeHashSet_EIU_ExceptWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        container.ExceptWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_UnsafeHashSet_EIU_ExceptWith_AxB()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new UnsafeHashSet<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.ExceptWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_UnsafeHashSet_EIU_IntersectWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        container.IntersectWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_UnsafeHashSet_EIU_IntersectWith()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new UnsafeHashSet<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.IntersectWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_UnsafeHashSet_EIU_UnionWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        container.UnionWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_UnsafeHashSet_EIU_UnionWith()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new UnsafeHashSet<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.UnionWith(other);

        ExpectedCount(ref container, 9);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));
        Assert.True(container.Contains(6));
        Assert.True(container.Contains(7));
        Assert.True(container.Contains(8));

        container.Dispose();
        other.Dispose();
    }


    [Test]
    public void UnsafeHashSet_NativeList_EIU_ExceptWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new NativeList<int>(8, Allocator.TempJob) { };
        container.ExceptWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_NativeList_EIU_ExceptWith_AxB()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new NativeList<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.ExceptWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_NativeList_EIU_IntersectWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new NativeList<int>(8, Allocator.TempJob) { };
        container.IntersectWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_NativeList_EIU_IntersectWith()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new NativeList<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.IntersectWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_NativeList_EIU_UnionWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new NativeList<int>(8, Allocator.TempJob) { };
        container.UnionWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_NativeList_EIU_UnionWith()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new NativeList<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.UnionWith(other);

        ExpectedCount(ref container, 9);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));
        Assert.True(container.Contains(6));
        Assert.True(container.Contains(7));
        Assert.True(container.Contains(8));

        container.Dispose();
        other.Dispose();
    }


    [Test]
    public void UnsafeHashSet_UnsafeList_EIU_ExceptWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new UnsafeList<int>(8, Allocator.TempJob) { };
        container.ExceptWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_UnsafeList_EIU_ExceptWith_AxB()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new UnsafeList<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.ExceptWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_UnsafeList_EIU_IntersectWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new UnsafeList<int>(8, Allocator.TempJob) { };
        container.IntersectWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_UnsafeList_EIU_IntersectWith()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new UnsafeList<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.IntersectWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_UnsafeList_EIU_UnionWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new UnsafeList<int>(8, Allocator.TempJob) { };
        container.UnionWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
        other.Dispose();
    }

    [Test]
    public void UnsafeHashSet_UnsafeList_EIU_UnionWith()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new UnsafeList<int>(8, Allocator.TempJob) { 3, 4, 5, 6, 7, 8 };
        container.UnionWith(other);

        ExpectedCount(ref container, 9);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));
        Assert.True(container.Contains(6));
        Assert.True(container.Contains(7));
        Assert.True(container.Contains(8));

        container.Dispose();
        other.Dispose();
    }


    [Test]
    public void UnsafeHashSet_FixedList32_EIU_ExceptWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList32<int>() { };
        container.ExceptWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList32_EIU_ExceptWith_AxB()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList32<int>() { 3, 4, 5, 6, 7, 8 };
        container.ExceptWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList32_EIU_IntersectWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList32<int>() { };
        container.IntersectWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList32_EIU_IntersectWith()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList32<int>() { 3, 4, 5, 6, 7, 8 };
        container.IntersectWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList32_EIU_UnionWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList32<int>() { };
        container.UnionWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList32_EIU_UnionWith()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList32<int>() { 3, 4, 5, 6, 7, 8 };
        container.UnionWith(other);

        ExpectedCount(ref container, 9);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));
        Assert.True(container.Contains(6));
        Assert.True(container.Contains(7));
        Assert.True(container.Contains(8));

        container.Dispose();
    }
    [Test]
    public void UnsafeHashSet_FixedList64_EIU_ExceptWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList64<int>() { };
        container.ExceptWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList64_EIU_ExceptWith_AxB()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList64<int>() { 3, 4, 5, 6, 7, 8 };
        container.ExceptWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList64_EIU_IntersectWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList64<int>() { };
        container.IntersectWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList64_EIU_IntersectWith()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList64<int>() { 3, 4, 5, 6, 7, 8 };
        container.IntersectWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList64_EIU_UnionWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList64<int>() { };
        container.UnionWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList64_EIU_UnionWith()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList64<int>() { 3, 4, 5, 6, 7, 8 };
        container.UnionWith(other);

        ExpectedCount(ref container, 9);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));
        Assert.True(container.Contains(6));
        Assert.True(container.Contains(7));
        Assert.True(container.Contains(8));

        container.Dispose();
    }
    [Test]
    public void UnsafeHashSet_FixedList128_EIU_ExceptWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList128<int>() { };
        container.ExceptWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList128_EIU_ExceptWith_AxB()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList128<int>() { 3, 4, 5, 6, 7, 8 };
        container.ExceptWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList128_EIU_IntersectWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList128<int>() { };
        container.IntersectWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList128_EIU_IntersectWith()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList128<int>() { 3, 4, 5, 6, 7, 8 };
        container.IntersectWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList128_EIU_UnionWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList128<int>() { };
        container.UnionWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList128_EIU_UnionWith()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList128<int>() { 3, 4, 5, 6, 7, 8 };
        container.UnionWith(other);

        ExpectedCount(ref container, 9);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));
        Assert.True(container.Contains(6));
        Assert.True(container.Contains(7));
        Assert.True(container.Contains(8));

        container.Dispose();
    }
    [Test]
    public void UnsafeHashSet_FixedList512_EIU_ExceptWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList512<int>() { };
        container.ExceptWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList512_EIU_ExceptWith_AxB()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList512<int>() { 3, 4, 5, 6, 7, 8 };
        container.ExceptWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList512_EIU_IntersectWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList512<int>() { };
        container.IntersectWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList512_EIU_IntersectWith()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList512<int>() { 3, 4, 5, 6, 7, 8 };
        container.IntersectWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList512_EIU_UnionWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList512<int>() { };
        container.UnionWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList512_EIU_UnionWith()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList512<int>() { 3, 4, 5, 6, 7, 8 };
        container.UnionWith(other);

        ExpectedCount(ref container, 9);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));
        Assert.True(container.Contains(6));
        Assert.True(container.Contains(7));
        Assert.True(container.Contains(8));

        container.Dispose();
    }
    [Test]
    public void UnsafeHashSet_FixedList4096_EIU_ExceptWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList4096<int>() { };
        container.ExceptWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList4096_EIU_ExceptWith_AxB()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList4096<int>() { 3, 4, 5, 6, 7, 8 };
        container.ExceptWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList4096_EIU_IntersectWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList4096<int>() { };
        container.IntersectWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList4096_EIU_IntersectWith()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList4096<int>() { 3, 4, 5, 6, 7, 8 };
        container.IntersectWith(other);

        ExpectedCount(ref container, 3);
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList4096_EIU_UnionWith_Empty()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { };
        var other = new FixedList4096<int>() { };
        container.UnionWith(other);

        ExpectedCount(ref container, 0);

        container.Dispose();
    }

    [Test]
    public void UnsafeHashSet_FixedList4096_EIU_UnionWith()
    {
        var container = new UnsafeHashSet<int>(8, Allocator.TempJob) { 0, 1, 2, 3, 4, 5 };
        var other = new FixedList4096<int>() { 3, 4, 5, 6, 7, 8 };
        container.UnionWith(other);

        ExpectedCount(ref container, 9);
        Assert.True(container.Contains(0));
        Assert.True(container.Contains(1));
        Assert.True(container.Contains(2));
        Assert.True(container.Contains(3));
        Assert.True(container.Contains(4));
        Assert.True(container.Contains(5));
        Assert.True(container.Contains(6));
        Assert.True(container.Contains(7));
        Assert.True(container.Contains(8));

        container.Dispose();
    }
}
