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
    }
}
