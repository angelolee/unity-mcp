using System;
using System.Collections.Generic;
using System.IO;
using com.tgs.mcpforunity.editor.Models;
using UnityEditor;

namespace com.tgs.mcpforunity.editor.Clients.Configurators
{
    public class ClaudeDesktopConfigurator : JsonFileMcpConfigurator
    {
        public const string ClientName = "Claude Desktop";

        public ClaudeDesktopConfigurator() : base(new McpClient
        {
            name = ClientName,
            windowsConfigPath = ResolveWindowsConfigPath(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)),
            macConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Claude", "claude_desktop_config.json"),
            linuxConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "Claude", "claude_desktop_config.json"),
            SupportsHttpTransport = false,
            StripEnvWhenNotRequired = true
        })
        { }

        public override bool SupportsSkills => true;

        public override string GetSkillInstallPath()
        {
            var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(userHome, ".claude", "skills", "unity-mcp-skill");
        }

        public override IList<string> GetInstallationSteps() => new List<string>
        {
            "Open Claude Desktop",
            "Go to Settings > Developer > Edit Config\nOR open the config path",
            "Paste the configuration JSON",
            "Save and restart Claude Desktop"
        };

        private static readonly ConfiguredTransport[] StdioOnly = { ConfiguredTransport.Stdio };
        public override IReadOnlyList<ConfiguredTransport> SupportedTransports => StdioOnly;

        internal static string ResolveWindowsConfigPath(string appDataPath, string localAppDataPath)
        {
            string standardPath = Path.Combine(appDataPath, "Claude", "claude_desktop_config.json");
            if (string.IsNullOrEmpty(localAppDataPath))
                return standardPath;

            try
            {
                string packagesDirectory = Path.Combine(localAppDataPath, "Packages");
                if (!Directory.Exists(packagesDirectory))
                    return standardPath;

                foreach (string packageDirectory in Directory.EnumerateDirectories(packagesDirectory))
                {
                    string packageName = Path.GetFileName(packageDirectory);
                    if (!packageName.StartsWith("Claude_", StringComparison.OrdinalIgnoreCase) &&
                        !packageName.StartsWith("Anthropic.ClaudeDesktop_", StringComparison.OrdinalIgnoreCase))
                        continue;

                    return Path.Combine(packageDirectory, "LocalCache", "Roaming", "Claude", "claude_desktop_config.json");
                }
            }
            catch { }

            return standardPath;
        }
    }
}
