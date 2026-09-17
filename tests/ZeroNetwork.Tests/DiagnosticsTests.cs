using System.Threading.Tasks;
using Xunit;
using ZeroNetwork.Diagnostics;

namespace ZeroNetwork.Tests
{
    public class DiagnosticsTests
    {
        [Fact]
        public async Task IsPortOpenAsync_ClosedPort_ReturnsFalse()
        {
            // Port 59999 is typically unused and closed
            bool isOpen = await NetworkProbe.IsPortOpenAsync("127.0.0.1", 59999, timeoutMs: 200);
            Assert.False(isOpen);
        }

        [Fact]
        public async Task PingAsync_Loopback_ReturnsSuccess()
        {
            var result = await NetworkProbe.PingAsync("127.0.0.1", timeoutMs: 1000);
            Assert.True(result.Success);
            Assert.True(result.RoundtripTimeMs >= 0);
        }
    }
}
