using NUnit.Framework;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;

namespace Unity.Collections.Tests
{
    /// <summary>
    /// Collections test fixture to do setup and teardown.
    /// </summary>
    /// <remarks>
    /// Jobs debugger and safety checks should always be enabled when running collections tests. This fixture verifies
    /// those are enabled to prevent crashing the editor.
    /// </remarks>
    internal abstract class CollectionsTestFixture
    {
        static string SafetyChecksMenu = "Jobs > Burst > Safety Checks";

#if !UNITY_DOTSRUNTIME
        private bool JobsDebuggerWasEnabled;
#endif

        [SetUp]
        public virtual void Setup()
        {
#if !UNITY_DOTSRUNTIME
            // Many ECS tests will only pass if the Jobs Debugger enabled;
            // force it enabled for all tests, and restore the original value at teardown.
            JobsDebuggerWasEnabled = JobsUtility.JobDebuggerEnabled;
            JobsUtility.JobDebuggerEnabled = true;
            Assert.IsTrue(BurstCompiler.Options.EnableBurstSafetyChecks, $"Collections tests must have Burst safety checks enabled! To enable, go to {SafetyChecksMenu}");
#endif
        }

        [TearDown]
        public virtual void TearDown()
        {
#if !UNITY_DOTSRUNTIME
            JobsUtility.JobDebuggerEnabled = JobsDebuggerWasEnabled;
#endif
        }
    }
}
