using System;
using System.Net.NetworkInformation;

namespace ZeroNetwork.Discovery
{
    /// <summary>
    /// Utility to accurately identify and filter out virtual, container, VPN, and hypervisor network interfaces.
    /// </summary>
    public static class VirtualAdapterFilter
    {
        private static readonly string[] VirtualKeywords = new[]
        {
            "hyper-v",
            "virtual",
            "vmware",
            "wsl",
            "vbox",
            "tap",
            "tun",
            "vpn",
            "loopback",
            "docker",
            "teredo",
            "isatap",
            "pseudo",
            "npcap",
            "winpcap",
            "wireguard",
            "tailscale",
            "zerotier",
            "hamachi",
            "cisco",
            "fortinet",
            "pulse",
            "checkpoint",
            "bluetooth"
        };

        /// <summary>
        /// Determines whether the specified network interface is a virtual, software-emulated, or tunnel adapter.
        /// </summary>
        public static bool IsVirtual(NetworkInterface nic)
        {
            if (nic == null) return true;

            // Loopback or tunnel interfaces are always virtual
            if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
            {
                return true;
            }

            string name = (nic.Name ?? string.Empty).ToLowerInvariant();
            string desc = (nic.Description ?? string.Empty).ToLowerInvariant();

            foreach (var kw in VirtualKeywords)
            {
                if (desc.IndexOf(kw, StringComparison.Ordinal) >= 0 ||
                    name.IndexOf(kw, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            // Check empty or dummy MAC
            string mac = nic.GetPhysicalAddress().ToString();
            if (string.IsNullOrWhiteSpace(mac) || mac == "000000000000")
            {
                return true;
            }

            return false;
        }
    }
}
