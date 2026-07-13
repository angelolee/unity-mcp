using com.tgs.mcpforunity.editor.Services.Transport;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MCPForUnityTests.Editor.Services
{
    [TestFixture]
    public class TransportManagerTests
    {
        [Test]
        public void IsRunning_UsesLiveClientStateWhenCachedStateIsConnected()
        {
            var client = new StaleTransportClient();
            var manager = new TransportManager();
            manager.Configure(
                () => client,
                () => client);

            manager.StartAsync(TransportMode.Stdio).GetAwaiter().GetResult();

            Assert.IsFalse(manager.IsRunning(TransportMode.Stdio));
            Assert.IsFalse(manager.GetState(TransportMode.Stdio).IsConnected);
        }

        private sealed class StaleTransportClient : IMcpTransportClient
        {
            public bool IsConnected => false;
            public string TransportName => "stdio";
            public TransportState State => TransportState.Connected(TransportName, port: 6401);

            public Task<bool> StartAsync() => Task.FromResult(true);
            public Task StopAsync() => Task.CompletedTask;
            public Task<bool> VerifyAsync() => Task.FromResult(false);
            public Task ReregisterToolsAsync() => Task.CompletedTask;
        }
    }
}
