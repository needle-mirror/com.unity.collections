using System;
using NUnit.Framework;

namespace Unity.Collections.Tests
{
#if UNITY_DOTSRUNTIME
    internal class DotsRuntimeIgnore : IgnoreAttribute
    {
        public DotsRuntimeIgnore(string msg="") : base("Need to fix for DotsRuntime.")
        {
        }
    }

#else
    internal class DotsRuntimeIgnore : Attribute
    {
        public DotsRuntimeIgnore(string msg="")
        {
        }
    }
#endif
}

// This is duplicated from Entities.
#if UNITY_PORTABLE_TEST_RUNNER
class IgnoreInPortableTests : IgnoreAttribute
{
    public IgnoreInPortableTests(string reason) : base(reason)
    {
    }
}
#else
class IgnoreInPortableTests : Attribute
{
    public IgnoreInPortableTests(string reason)
    {
    }
}
#endif

