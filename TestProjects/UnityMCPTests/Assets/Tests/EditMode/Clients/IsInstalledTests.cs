using System;
using System.IO;
using com.tgs.mcpforunity.editor.Clients;
using com.tgs.mcpforunity.editor.Clients.Configurators;
using NUnit.Framework;

namespace MCPForUnityTests.Editor.Clients
{
    [TestFixture]
    public class IsInstalledTests
    {
        [Test]
        public void IMcpClientConfigurator_ExposesIsInstalled()
        {
            var prop = typeof(IMcpClientConfigurator).GetProperty("IsInstalled");
            Assert.IsNotNull(prop, "IMcpClientConfigurator must expose an IsInstalled property");
            Assert.AreEqual(typeof(bool), prop.PropertyType);
        }

        [Test]
        public void JsonClient_NotInstalled_WhenParentDirMissing()
        {
            var cursor = new CursorConfigurator();
            string parent = Path.GetDirectoryName(cursor.GetConfigPath());
            if (parent == null || !Directory.Exists(parent))
            {
                Assert.IsFalse(cursor.IsInstalled,
                    "Cursor parent dir does not exist on this machine, IsInstalled must be false");
            }
            else
            {
                Assert.IsTrue(cursor.IsInstalled,
                    "Cursor parent dir exists, IsInstalled must be true");
            }
        }

        [Test]
        public void JsonClient_Installed_WhenParentDirExists()
        {
            var claude = new ClaudeDesktopConfigurator();
            string parent = Path.GetDirectoryName(claude.GetConfigPath());
            bool expected = parent != null && Directory.Exists(parent);
            Assert.AreEqual(expected, claude.IsInstalled);
        }

        [Test]
        public void ClaudeDesktopConfigPath_PrefersMsixVirtualizedConfig()
        {
            string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            string appData = Path.Combine(root, "Roaming");
            string localAppData = Path.Combine(root, "Local");
            string packageRoot = Path.Combine(localAppData, "Packages", "Claude_pzs8sxrjxfjjc");
            string expected = Path.Combine(packageRoot, "LocalCache", "Roaming", "Claude", "claude_desktop_config.json");

            Directory.CreateDirectory(packageRoot);
            try
            {
                Assert.AreEqual(expected, ClaudeDesktopConfigurator.ResolveWindowsConfigPath(appData, localAppData));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }
    }
}
