using System.Net;
using Xunit;
using ZeroNetwork.Common;
using ZeroNetwork.Discovery;
using ZeroNetwork.Sockets;

namespace ZeroNetwork.Tests
{
    public class ArpAndWolTests
    {
        [Fact]
        public void WakeOnLan_CreateMagicPacket_ValidStructure()
        {
            var mac = MacAddress.Parse("00:11:22:33:44:55");
            byte[] packet = WakeOnLan.CreateMagicPacket(mac);

            Assert.Equal(102, packet.Length);

            // First 6 bytes are 0xFF
            for (int i = 0; i < 6; i++)
            {
                Assert.Equal(0xFF, packet[i]);
            }

            // 16 repetitions of 00 11 22 33 44 55
            byte[] expectedMac = mac.GetAddressBytes();
            for (int rep = 0; rep < 16; rep++)
            {
                int offset = 6 + (rep * 6);
                for (int b = 0; b < 6; b++)
                {
                    Assert.Equal(expectedMac[b], packet[offset + b]);
                }
            }
        }

        [Fact]
        public void ArpTable_GetTable_DoesNotThrow()
        {
            var table = ArpTable.GetTable();
            Assert.NotNull(table);
            // May be empty on headless runner or non-empty on workstation
        }

        [Fact]
        public void UdpMulticastClient_CreateAndLifecycle_Succeeds()
        {
            using (var client = new UdpMulticastClient(0, IPAddress.Loopback))
            {
                client.StartListening();
                Assert.True(client.IsListening);
                client.StopListening();
                Assert.False(client.IsListening);
            }
        }
    }
}
