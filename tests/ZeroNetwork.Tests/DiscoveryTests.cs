using System;
using System.Net;
using Xunit;
using ZeroNetwork.Discovery;

namespace ZeroNetwork.Tests
{
    public class DiscoveryTests
    {
        [Fact]
        public void GetLocalIPv4_ReturnsValidIp()
        {
            string ip = NetworkInfo.GetLocalIPv4();

            Assert.False(string.IsNullOrWhiteSpace(ip));
            Assert.True(IPAddress.TryParse(ip, out var parsed));
            Assert.Equal(System.Net.Sockets.AddressFamily.InterNetwork, parsed.AddressFamily);
        }

        [Fact]
        public void GetAllActiveIPv4_DoesNotContainLoopback()
        {
            var ips = NetworkInfo.GetAllActiveIPv4(excludeVirtual: true);

            Assert.NotNull(ips);
            foreach (var ip in ips)
            {
                Assert.True(IPAddress.TryParse(ip, out var parsed));
                Assert.False(IPAddress.IsLoopback(parsed), $"IP {ip} should not be loopback");
            }
        }

        [Fact]
        public void GetPhysicalMacAddress_ReturnsFormattedString()
        {
            string mac = NetworkInfo.GetPhysicalMacAddress("-");

            if (!string.IsNullOrEmpty(mac))
            {
                var parts = mac.Split('-');
                Assert.Equal(6, parts.Length);
                foreach (var part in parts)
                {
                    Assert.Equal(2, part.Length);
                    Assert.True(byte.TryParse(part, System.Globalization.NumberStyles.HexNumber, null, out _));
                }
            }
        }

        [Fact]
        public void GetAdapters_ReturnsNonEmptyList()
        {
            var adapters = NetworkInfo.GetAdapters(operationalOnly: false);

            Assert.NotNull(adapters);
            Assert.NotEmpty(adapters);
        }
    }
}
