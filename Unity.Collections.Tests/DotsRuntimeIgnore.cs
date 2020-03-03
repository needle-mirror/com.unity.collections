using System;
using NUnit.Framework;

namespace Unity.Collections.Tests
{
#if UNITY_DOTSPLAYER
    public class DotsRuntimeIgnore : IgnoreAttribute
    {
        public DotsRuntimeIgnore() : base("Need to fix for DotsRuntime.")
        {
        }
    }
#else
    public class DotsRuntimeIgnore : Attribute
    {
    }
#endif
}