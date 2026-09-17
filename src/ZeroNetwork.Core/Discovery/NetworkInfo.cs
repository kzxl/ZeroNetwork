using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using ZeroNetwork.Common;

namespace ZeroNetwork.Discovery
{
    /// <summary>
    /// Provides discovery and inspection of local network interfaces, physical MAC addresses, and IP configurations.
    /// </summary>
    public static class NetworkInfo
    {
        private const string LoopbackIPv4 = "127.0.0.1";

        /// <summary>
        /// Gets the primary operational local IPv4 address, filtering out loopback and virtual adapters (WSL, Hyper-V, VMware, VPN).
        /// Prioritizes physical Ethernet, then Wireless.
        /// </summary>
        public static string GetLocalIPv4(NetworkInterfaceType? preferredType = null)
        {
            try
            {
                var nics = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up)
                    .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    .ToList();

                if (nics.Count == 0) return LoopbackIPv4;

                // Sort: PreferredType first -> Physical Ethernet -> Wireless -> Other physical -> Virtual fallback
                var sortedNics = nics
                    .OrderByDescending(n => preferredType.HasValue && n.NetworkInterfaceType == preferredType.Value && !VirtualAdapterFilter.IsVirtual(n))
                    .ThenByDescending(n => n.NetworkInterfaceType == NetworkInterfaceType.Ethernet && !VirtualAdapterFilter.IsVirtual(n))
                    .ThenByDescending(n => n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && !VirtualAdapterFilter.IsVirtual(n))
                    .ThenByDescending(n => !VirtualAdapterFilter.IsVirtual(n))
                    .ToList();

                foreach (var nic in sortedNics)
                {
                    var ipProps = nic.GetIPProperties();
                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addr.Address))
                        {
                            return addr.Address.ToString();
                        }
                    }
                }

                // Fallback using Dns.GetHostAddresses
                foreach (var ip in Dns.GetHostAddresses(Dns.GetHostName()))
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }

                return LoopbackIPv4;
            }
            catch
            {
                return LoopbackIPv4;
            }
        }

        /// <summary>
        /// Gets all active IPv4 addresses assigned to the machine, with optional filtering of virtual adapters.
        /// </summary>
        public static List<string> GetAllActiveIPv4(bool excludeVirtual = true)
        {
            var result = new List<string>();
            try
            {
                var nics = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up)
                    .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel);

                if (excludeVirtual)
                {
                    nics = nics.Where(n => !VirtualAdapterFilter.IsVirtual(n));
                }

                foreach (var nic in nics)
                {
                    var ipProps = nic.GetIPProperties();
                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addr.Address))
                        {
                            string ipStr = addr.Address.ToString();
                            if (!result.Contains(ipStr))
                            {
                                result.Add(ipStr);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Suppress network inspection exceptions
            }

            return result;
        }

        /// <summary>
        /// Gets the MAC address of the primary active physical network card.
        /// </summary>
        /// <param name="separator">Delimiter between bytes (e.g. "-", ":", or "" for continuous string).</param>
        public static string GetPhysicalMacAddress(string separator = "-")
        {
            try
            {
                var nic = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up)
                    .Where(n => !VirtualAdapterFilter.IsVirtual(n))
                    .OrderByDescending(n => n.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    .ThenByDescending(n => n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    .FirstOrDefault();

                if (nic == null) return string.Empty;

                byte[] bytes = nic.GetPhysicalAddress().GetAddressBytes();
                if (bytes == null || bytes.Length == 0) return string.Empty;

                var sb = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    sb.Append(bytes[i].ToString("X2"));
                    if (i < bytes.Length - 1 && !string.IsNullOrEmpty(separator))
                    {
                        sb.Append(separator);
                    }
                }
                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Retrieves detailed information of all network adapter interfaces.
        /// </summary>
        public static List<NetworkAdapterInfo> GetAdapters(bool operationalOnly = true, bool excludeVirtual = false)
        {
            var result = new List<NetworkAdapterInfo>();
            try
            {
                var nics = NetworkInterface.GetAllNetworkInterfaces().AsEnumerable();

                if (operationalOnly)
                {
                    nics = nics.Where(n => n.OperationalStatus == OperationalStatus.Up);
                }

                foreach (var nic in nics)
                {
                    bool isVirtual = VirtualAdapterFilter.IsVirtual(nic);
                    if (excludeVirtual && isVirtual) continue;

                    var info = new NetworkAdapterInfo
                    {
                        Id = nic.Id,
                        Name = nic.Name,
                        Description = nic.Description,
                        InterfaceType = nic.NetworkInterfaceType,
                        Status = nic.OperationalStatus,
                        Speed = nic.Speed,
                        IsVirtual = isVirtual
                    };

                    byte[] macBytes = nic.GetPhysicalAddress().GetAddressBytes();
                    if (macBytes != null && macBytes.Length > 0)
                    {
                        info.RawMacAddress = string.Concat(macBytes.Select(b => b.ToString("X2")));
                        info.MacAddress = string.Join("-", macBytes.Select(b => b.ToString("X2")));
                    }

                    try
                    {
                        var ipProps = nic.GetIPProperties();
                        foreach (var addr in ipProps.UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                info.IPv4Addresses.Add(addr.Address.ToString());
                            }
                            else if (addr.Address.AddressFamily == AddressFamily.InterNetworkV6)
                            {
                                info.IPv6Addresses.Add(addr.Address.ToString());
                            }
                        }

                        foreach (var gw in ipProps.GatewayAddresses)
                        {
                            if (gw.Address != null && !string.IsNullOrEmpty(gw.Address.ToString()))
                            {
                                info.Gateways.Add(gw.Address.ToString());
                            }
                        }

                        foreach (var dns in ipProps.DnsAddresses)
                        {
                            info.DnsAddresses.Add(dns.ToString());
                        }
                    }
                    catch
                    {
                        // Some virtual or disabled adapters throw when accessing IP properties
                    }

                    result.Add(info);
                }
            }
            catch
            {
                // Suppress enumeration failures
            }

            return result;
        }

        /// <summary>
        /// Gets the primary Default Gateway IPv4 address.
        /// </summary>
        public static string GetDefaultGateway()
        {
            try
            {
                var adapters = GetAdapters(operationalOnly: true, excludeVirtual: true);
                foreach (var adapter in adapters)
                {
                    if (adapter.Gateways.Count > 0)
                    {
                        return adapter.Gateways[0];
                    }
                }
            }
            catch
            {
                // Ignore
            }

            return string.Empty;
        }
    }
}
