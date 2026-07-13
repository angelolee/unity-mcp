using System;
using System.Diagnostics;

namespace com.tgs.mcpforunity.editor.Security
{
    /// <summary>
    /// Linux libsecret-backed key store via the `secret-tool` CLI. The secret value is
    /// passed on stdin (never as a process argument). Falls back to
    /// <see cref="EncryptedFileKeyStore"/> when secret-tool is not installed.
    /// </summary>
    internal sealed class LinuxSecretToolKeyStore : ISecureKeyStore
    {
        private const string Service = SecureKeyStoreConstants.ServiceName;

        internal static bool IsAvailable()
        {
            try
            {
                var psi = NewPsi("--version");
                using (var p = Process.Start(psi))
                {
                    p.WaitForExit(2000);
                    return p.ExitCode == 0;
                }
            }
            catch { return false; }
        }

        public bool Has(string providerId) => TryGet(providerId, out _);

        public bool TryGet(string providerId, out string apiKey)
        {
            apiKey = null;
            if (string.IsNullOrEmpty(providerId)) return false;
            try
            {
                var psi = NewPsi("lookup", "service", Service, "account", providerId);
                using (var p = Process.Start(psi))
                {
                    string outp = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(5000);
                    if (p.ExitCode != 0) return false;
                    apiKey = (outp ?? string.Empty).TrimEnd('\n', '\r');
                    return !string.IsNullOrEmpty(apiKey);
                }
            }
            catch { return false; }
        }

        public void Set(string providerId, string apiKey)
        {
            if (string.IsNullOrEmpty(providerId)) return;
            if (string.IsNullOrEmpty(apiKey)) { Delete(providerId); return; }
            try
            {
                var psi = NewPsi(true, "store", "--label=MCPForUnity AssetGen", "service", Service, "account", providerId);
                using (var p = Process.Start(psi))
                {
                    p.StandardInput.Write(apiKey);
                    p.StandardInput.Close();
                    p.WaitForExit(5000);
                }
            }
            catch { /* best effort */ }
        }

        public void Delete(string providerId)
        {
            if (string.IsNullOrEmpty(providerId)) return;
            try
            {
                var psi = NewPsi("clear", "service", Service, "account", providerId);
                using (var p = Process.Start(psi)) p.WaitForExit(5000);
            }
            catch { /* best effort */ }
        }

        private static ProcessStartInfo NewPsi(params string[] args)
        {
            return NewPsi(false, args);
        }

        private static ProcessStartInfo NewPsi(bool redirectIn, params string[] args)
        {
            var psi = new ProcessStartInfo("/usr/bin/env")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = redirectIn,
                Arguments = ProcessArgumentFormatter.Join(PrependCommand(args)),
            };
            return psi;
        }

        private static string[] PrependCommand(string[] args)
        {
            var result = new string[args.Length + 1];
            result[0] = "secret-tool";
            Array.Copy(args, 0, result, 1, args.Length);
            return result;
        }
    }
}
