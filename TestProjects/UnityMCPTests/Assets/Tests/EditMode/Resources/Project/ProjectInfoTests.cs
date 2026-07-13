using System.Linq;
using System.Runtime.CompilerServices;
using com.tgs.mcpforunity.editor.Helpers;
using com.tgs.mcpforunity.editor.Resources.Project;
using com.tgs.mcpforunity.Serialization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace MCPForUnityTests.Editor.Resources.Project
{
    public class ProjectInfoTests
    {
        [Test]
        public void HandleCommand_ReportsScreenCaptureAvailability()
        {
            var response = ProjectInfo.HandleCommand(new JObject()) as SuccessResponse;

            Assert.IsNotNull(response);
            var data = JObject.FromObject(response.Data);
            Assert.IsTrue(data["packages"]?["screenCapture"]?.Value<bool>() ?? false);
        }

        [Test]
        public void RuntimeAssembly_GrantsEditorAssemblyAccessToInternalConverters()
        {
            var friends = typeof(Vector3Converter).Assembly
                .GetCustomAttributes(typeof(InternalsVisibleToAttribute), false)
                .Cast<InternalsVisibleToAttribute>();

            Assert.That(friends.Select(friend => friend.AssemblyName),
                Does.Contain("com.tgs.mcp-for-unity.editor"));
        }
    }
}
