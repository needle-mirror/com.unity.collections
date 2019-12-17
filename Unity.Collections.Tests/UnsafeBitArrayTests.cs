using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public class UnsafeBitArrayTests
{
    [Test]
    public void UnsafeBitArray_Get_Set()
    {
        var numBits = 256;

        var test = new UnsafeBitArray(numBits, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        Assert.False(test.IsSet(123));
        test.Set(123, true);
        Assert.True(test.IsSet(123));

        Assert.False(test.TestAll(0, numBits));
        Assert.False(test.TestNone(0, numBits));
        Assert.True(test.TestAny(0, numBits));
        Assert.AreEqual(1, test.CountBits(0, numBits));

        Assert.False(test.TestAll(0, 122));
        Assert.True(test.TestNone(0, 122));
        Assert.False(test.TestAny(0, 122));

        test.Clear();
        Assert.False(test.IsSet(123));
        Assert.AreEqual(0, test.CountBits(0, numBits));

        test.SetBits(0, true, numBits);
        Assert.False(test.TestNone(0, numBits));
        Assert.True(test.TestAll(0, numBits));

        test.SetBits(0, false, numBits);
        Assert.True(test.TestNone(0, numBits));
        Assert.False(test.TestAll(0, numBits));

        test.SetBits(123, true, 7);
        Assert.True(test.TestAll(123, 7));

        test.Dispose();
    }

    [Test]
    public unsafe void UnsafeBitArray_Throws()
    {
        var numBits = 256;

        var test = new UnsafeBitArray(numBits, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        Assert.DoesNotThrow(() => { test.TestAll(0, numBits); });
        Assert.Throws<ArgumentException>(() => { test.IsSet(-1); });
        Assert.Throws<ArgumentException>(() => { test.IsSet(numBits); });

        Assert.Throws<ArgumentException>(() => { new UnsafeBitArray(null, 7); /* check sizeInBytes must be multiple of 8-bytes. */ });

        test.Dispose();
    }
}