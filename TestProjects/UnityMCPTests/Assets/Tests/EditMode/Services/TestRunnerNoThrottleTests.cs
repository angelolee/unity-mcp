using System.Reflection;
using NUnit.Framework;
using MCPForUnity.Editor.Services;

namespace MCPForUnityTests.Editor.Services
{
    public class TestRunnerNoThrottleTests
    {
        [Test]
        public void ShouldClearConsoleAfterRun_RequiresUnity2020AndZeroFailures()
        {
            var method = typeof(TestRunnerNoThrottle).GetMethod(
                "ShouldClearConsoleAfterRun",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method, "Expected TestRunnerNoThrottle.ShouldClearConsoleAfterRun helper.");

            Assert.IsFalse((bool)method.Invoke(null, new object[] { false, 0 }),
                "Non-2020 Unity versions should not get post-run console cleanup.");
            Assert.IsFalse((bool)method.Invoke(null, new object[] { true, 1 }),
                "Failed test runs should keep console logs for diagnosis.");
            Assert.IsTrue((bool)method.Invoke(null, new object[] { true, 0 }),
                "Passing Unity 2020 test runs should clean up native allocator console noise.");
        }

#if !UNITY_2021_1_OR_NEWER
        [Test]
        public void ShouldSuspendNativeLeakDetectionDuringRun_IsLimitedToUnity2020()
        {
            var method = typeof(TestRunnerNoThrottle).GetMethod(
                "ShouldSuspendNativeLeakDetectionDuringRun",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method, "Expected native leak detection suppression decision helper.");
            Assert.IsFalse((bool)method.Invoke(null, new object[] { false }));
            Assert.IsTrue((bool)method.Invoke(null, new object[] { true }));
        }

        [Test]
        public void ShouldRestoreNativeLeakDetectionImmediatelyAfterRun_DefersUnity2020PassingRuns()
        {
            var method = typeof(TestRunnerNoThrottle).GetMethod(
                "ShouldRestoreNativeLeakDetectionImmediatelyAfterRun",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method, "Expected native leak detection restore decision helper.");
            Assert.IsTrue((bool)method.Invoke(null, new object[] { false, 0 }),
                "Newer Unity versions should restore leak detection after passing runs.");
            Assert.IsTrue((bool)method.Invoke(null, new object[] { true, 1 }),
                "Failed Unity 2020 runs should restore leak detection for diagnostics.");
            Assert.IsFalse((bool)method.Invoke(null, new object[] { true, 0 }),
                "Passing Unity 2020 runs should keep native leak detection disabled to avoid false native TLS allocator spam.");
        }

        [Test]
        public void ShouldRestoreTestSettingsAfterPostRunCleanup_IsOnlyForUnity2020PassingRuns()
        {
            var method = typeof(TestRunnerNoThrottle).GetMethod(
                "ShouldRestoreTestSettingsAfterPostRunCleanup",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method, "Expected delayed test settings restore decision helper.");
            Assert.IsFalse((bool)method.Invoke(null, new object[] { false, 0 }),
                "Newer Unity versions should restore immediately instead of using the cleanup path.");
            Assert.IsFalse((bool)method.Invoke(null, new object[] { true, 1 }),
                "Failed Unity 2020 runs should restore immediately so native diagnostics remain visible.");
            Assert.IsTrue((bool)method.Invoke(null, new object[] { true, 0 }),
                "Passing Unity 2020 runs should restore after post-run console cleanup.");
        }
#endif
    }
}
