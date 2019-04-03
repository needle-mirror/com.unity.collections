using NUnit.Framework;
using System;
using Unity.Collections;

public class ResizableArrayTests
{
    [Test]
    public void TestResizableArray64Byte()
    {    
        ResizableArray64Byte<int> array = new ResizableArray64Byte<int>();
        for(int i = 0; i < array.Capacity; ++i)
            array.Add(i);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        Assert.Throws<IndexOutOfRangeException>(() => { array.Add((int)array.Capacity); });
#endif
        for(int i = 0; i < array.Capacity; ++i)
            Assert.AreEqual(array[i], i);
    }
}
