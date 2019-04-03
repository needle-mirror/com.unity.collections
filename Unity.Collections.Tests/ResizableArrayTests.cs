using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections;

public class ResizableArrayTests
{
    [Test]
    public void TestResizableArray64Byte()
    {
        var array = new ResizableArray64Byte<int>();
        for(var i = 0; i < array.Capacity; ++i)
            array.Add(i);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        Assert.Throws<IndexOutOfRangeException>(() => array.Add(array.Capacity));
#endif
        for(var i = 0; i < array.Capacity; ++i)
            Assert.AreEqual(array[i], i);
    }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    struct LargeStruct { int i0, i1, i2, i3, i4, i5, i6; }

    [Test, SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    public void CtorCalled_WithDataBeyondCapacity_Throws()
    {
        var (a, b, c) = (new LargeStruct(), new LargeStruct(), new LargeStruct());
        Assert.DoesNotThrow(() => new ResizableArray64Byte<LargeStruct>(a, b));
        Assert.Throws<IndexOutOfRangeException>(() => new ResizableArray64Byte<LargeStruct>(a, b, c));
    }
#endif
}
