#if UNITY_EDITOR
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
