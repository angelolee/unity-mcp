using com.tgs.mcpforunity.editor.Services;
using NUnit.Framework;

namespace MCPForUnityTests.Editor.Services
{
    public class PathResolverServiceTests
    {
        [TestCase(true, "uvx.exe")]
        [TestCase(false, "uvx")]
        public void UvxCommandNames_RequireUvxExecutable(bool isWindows, string expectedCommand)
        {
            CollectionAssert.AreEqual(
                new[] { expectedCommand },
                PathResolverService.GetUvxCommandNamesForPlatform(isWindows));
        }
    }
}
