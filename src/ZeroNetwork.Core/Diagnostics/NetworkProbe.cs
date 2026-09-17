using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroNetwork.Diagnostics
{
    /// <summary>
    /// Result structure for ping diagnostics.
    /// </summary>
    public readonly struct PingProbeResult
    {
        public bool Success { get; }
        public long RoundtripTimeMs { get; }
        public IPStatus Status { get; }
        public string Address { get; }

        public PingProbeResult(bool success, long roundtripTimeMs, IPStatus status, string address)
        {
            Success = success;
            RoundtripTimeMs = roundtripTimeMs;
            Status = status;
            Address = address;
        }

        public static PingProbeResult Failed(IPStatus status = IPStatus.Unknown, string address = "")
            => new PingProbeResult(false, -1, status, address);
    }

    /// <summary>
    /// High-performance network connectivity diagnostics and probing tools.
    /// </summary>
    public static class NetworkProbe
    {
        /// <summary>
        /// Asynchronously tests whether a specific TCP host and port is accepting connections within the specified timeout.
        /// Guaranteed non-blocking and safe for UI threads.
        /// </summary>
        /// <param name="host">Target IP or hostname.</param>
        /// <param name="port">Target TCP port.</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 1000ms).</param>
        /// <returns>True if connection succeeded; otherwise false.</returns>
        public static async Task<bool> IsPortOpenAsync(string host, int port, int timeoutMs = 1000)
        {
            if (string.IsNullOrWhiteSpace(host) || port <= 0 || port > 65535)
                return false;

            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(host, port);
                    var timeoutTask = Task.Delay(timeoutMs);

                    var completedTask = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);
                    if (completedTask == connectTask)
                    {
                        // Check if an exception occurred during connection
                        await connectTask.ConfigureAwait(false);
                        return client.Connected;
                    }

                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Asynchronously sends an ICMP echo ping to the specified host.
        /// </summary>
        /// <param name="host">Target IP or hostname.</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 1000ms).</param>
        /// <returns>PingProbeResult containing roundtrip time and status.</returns>
        public static async Task<PingProbeResult> PingAsync(string host, int timeoutMs = 1000)
        {
            if (string.IsNullOrWhiteSpace(host))
                return PingProbeResult.Failed(address: host);

            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(host, timeoutMs).ConfigureAwait(false);
                    if (reply != null && reply.Status == IPStatus.Success)
                    {
                        return new PingProbeResult(
                            true,
                            reply.RoundtripTime,
                            reply.Status,
                            reply.Address?.ToString() ?? host);
                    }

                    return PingProbeResult.Failed(reply?.Status ?? IPStatus.Unknown, host);
                }
            }
            catch
            {
                return PingProbeResult.Failed(address: host);
            }
        }
    }
}
