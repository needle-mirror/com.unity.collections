#if UNITY_EDITOR && !UNITY_2020_2_OR_NEWER
// disable on 2020.2 until DOTS-2592 is resolved
using NUnit.Framework;
using Unity.Collections.Tests;

[TestFixture, EmbeddedPackageOnlyTest]
public class CollectionsBurstTests : BurstCompatibilityTests
{
    public CollectionsBurstTests()
        : base("Packages/com.unity.collections/Unity.Collections.Tests/_generated_burst_tests.cs",
            "Unity.Collections")
    {
    }
}
#endif
