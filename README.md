# ZeroNetwork

[![NuGet Version](https://img.shields.io/badge/nuget-v2.2.0-blue.svg)](https://www.nuget.org/packages/ZeroNetwork.Core/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Zero External Dependencies](https://img.shields.io/badge/Dependencies-0%20(Pure%20C%23)-brightgreen.svg)]()
[![Tests: 31 Passed](https://img.shields.io/badge/Tests-31%20Passed%20(100%25)-brightgreen.svg)]()
[![Multi-Targeting](https://img.shields.io/badge/.NET-8.0%20%7C%204.6.2%20%7C%20Standard%202.0-orange.svg)]()

> **Architectural Standard**: 100% Pure C# BCL, Zero External Dependencies, Multi-Targeting across `.NET 8.0`, `.NET Framework 4.6.2`, and `.NET Standard 2.0`.

`ZeroNetwork` is an ultra-high-performance, sovereign .NET networking suite engineered for factory floor automation, IoT edge gateways, distributed industrial systems, and high-load ERP infrastructures. It provides an end-to-end networking stack from **Layer 2 (Data Link & Hardware)** all the way up to **Layer 7 (HTTP REST & Real-Time SignalR/WebSocket)** with **ZERO external NuGet dependencies**, eliminating assembly conflicts and DLL hell on legacy .NET Framework runtimes while maximizing throughput on modern .NET 8+.

---

## 🏛️ Comprehensive Architecture & OSI Model Mapping

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                               ZeroNetwork Architecture                          │
├───────────────────┬──────────────────────────────────┬──────────────────────────┤
│ OSI Layer         │ Component                        │ Key Highlights           │
├───────────────────┼──────────────────────────────────┼──────────────────────────┤
│ Layer 7: RealTime │ ZeroSignalRClient                │ Pure C# Hub Protocol v1  │
│                   │ ZeroWebSocketClient              │ RFC 6455, Auto-Reconnect │
├───────────────────┼──────────────────────────────────┼──────────────────────────┤
│ Layer 7: HTTP     │ ZeroApiClient & Extensions       │ Zero-LOH Stream Pipeline │
│                   │ ZeroHttpServer                   │ Micro REST & Prometheus  │
├───────────────────┼──────────────────────────────────┼──────────────────────────┤
│ Layer 4: Transport│ NetworkProbe & PortScanner       │ Zero-Leak TCP Handshake  │
│                   │ UdpMulticastClient & DnsProbe    │ IGMPv2/v3, Port 53 UDP   │
│                   │ NetworkThroughputMeter           │ Real-Time NIC Bandwidth  │
├───────────────────┼──────────────────────────────────┼──────────────────────────┤
│ Layer 3: Network  │ IPNetwork & VirtualAdapterFilter │ Allocation-Free CIDR     │
│                   │ Traceroute & PathMtuDiscovery    │ Hop Latency & Jumbo PMTU │
│                   │ NetworkInfo & NetworkAdapterInfo │ Deterministic NIC Select │
├───────────────────┼──────────────────────────────────┼──────────────────────────┤
│ Layer 2: DataLink │ MacAddress & ArpTable            │ Blittable Struct, P/Invk │
│                   │ ActiveArpScanner                 │ Subnet Scanner (No ICMP) │
│                   │ WakeOnLan                        │ 102/108-byte Magic Pkt   │
└───────────────────┴──────────────────────────────────┴──────────────────────────┘
```

---

## 🌟 Key Capabilities

### 1. Layer 2: Data Link & Hardware Discovery
- **`MacAddress`**: Blittable 6-byte struct with zero-allocation span formatting (`Span<char>`) and IEEE OUI vendor detection (VMware, VirtualBox, Hyper-V, Docker, QEMU).
- **`ArpTable`**: Fast IP $\leftrightarrow$ MAC resolution via Windows P/Invoke (`IpHlpApi.dll`) and Linux `/proc/net/arp`.
- **`ActiveArpScanner`**: High-speed parallel ARP sweep across CIDR subnets to detect 100% of live nodes even when ICMP/Ping is disabled by firewalls.
- **`WakeOnLan`**: Generates RFC standard 102-byte Magic Packets with support for 6-byte `SecureOn` passwords (108-byte packets) for remote machine power-on.

### 2. Layer 3: Network & Topology Math
- **`IPNetwork`**: Pure C# CIDR math (`Parse`, `Contains`, `BroadcastAddress`, `Netmask`, `WildcardMask`) with zero-allocation host iterators using `Span<byte>`.
- **`VirtualAdapterFilter`**: Hybrid heuristics and hardware vendor OUI detection that filters out virtual interfaces (VMware, VirtualBox, WSL, Hyper-V, Tailscale, ZeroTier, Docker).
- **`NetworkInfo` & `NetworkAdapterInfo`**: Deterministic physical NIC resolution on multi-homed systems.
- **`Traceroute`**: Hop-by-hop ICMP route inspection with latency profiling.
- **`PathMtuDiscovery`**: Binary search PMTU analyzer using Don't Fragment (DF) flags (verifying Jumbo Frames MTU 9000 for GigE Vision cameras).

### 3. Layer 4: Transport & Diagnostics
- **`NetworkProbe`**: Hardened non-blocking TCP socket prober (`IsPortOpenAsync`) with `LingerState(true, 0)` socket abort ensuring zero socket handle leaks under heavy loads.
- **`PortScanner`**: Parallel bounded-concurrency TCP port and subnet scanner governed by `SemaphoreSlim` with streaming `Action<PortScanResult>` callbacks.
- **`UdpMulticastClient`**: IGMPv2/v3 multicast group publisher and subscriber with physical network interface binding.
- **`DnsProbe`**: Fast UDP port 53 DNS client with sub-second timeout and reverse PTR record resolution.
- **`NetworkThroughputMeter`**: Real-time NIC bandwidth meter (Rx/Tx Mbps) with saturation alert thresholds.

### 4. Layer 7: Micro HTTP Server & REST Client
- **`ZeroHttpServer`**: Micro HTTP/1.1 server running in pure C# without administrator privileges, URL ACLs, or IIS. Features regex REST routing and built-in `/metrics` (Prometheus) and `/health` endpoints.
- **`ZeroApiClient`**: High-performance HTTP client utilizing stream-based direct JSON serialization (`IZeroJsonSerializer`) to eliminate Large Object Heap (LOH) allocations, combined with connection pooling and exponential backoff retry.
- **`ZeroHttpExtensions`**: Fluent string extensions (`url.GetJsonAsync<T>()`, `url.PostJsonAsync<T>()`) for clean, allocation-efficient HTTP consumption.

### 5. Layer 7: Real-Time Full-Duplex (WebSocket & SignalR)
- **`ZeroWebSocketClient`**: Resilient RFC 6455 WebSocket client built purely on BCL `ClientWebSocket`. Includes thread-safe frame sending (`SemaphoreSlim`), automatic message reassembly for large payloads, configurable keep-alive heartbeat, and auto-reconnect with exponential backoff and random jitter.
- **`ZeroSignalRClient`**: Pure C# ASP.NET Core SignalR JSON Hub Client (Protocol v1) with **0 external dependencies** (no `Microsoft.AspNetCore.SignalR.Client` bloat, eliminating DLL conflicts on .NET Framework 4.6.2). Supports automatic handshake, server-to-client callbacks (`hub.On<T>`), client invocations (`hub.SendAsync`, `hub.InvokeAsync<T>`), and keep-alive ping management.

---

## 🚀 Quick Start Examples

### 1. Active ARP Subnet Scanning (Bypass ICMP Firewalls)
```csharp
using ZeroNetwork.Discovery;

// Scan /24 subnet for all active devices via Data Link ARP
var devices = await ActiveArpScanner.ScanSubnetAsync("192.168.1.0/24", timeoutMs: 300);

foreach (var device in devices)
{
    Console.WriteLine($"IP: {device.IpAddress} | MAC: {device.MacAddress} | Vendor: {device.Vendor}");
}
```

### 2. High-Performance REST Client (Zero-LOH Stream Pipeline)
```csharp
using ZeroNetwork.Http;

// Fluent API with automatic connection pooling and retry
var users = await "https://api.factory.local/users".GetJsonAsync<List<UserDto>>();

// Or using ZeroApiClient directly
using var client = new ZeroApiClient(new ZeroApiClientOptions
{
    BaseAddress = new Uri("https://api.factory.local"),
    MaxRetries = 3
});

var response = await client.PostJsonAsync<OrderRequest, OrderResult>("/orders", new OrderRequest { OrderId = 1001 });
```

### 3. Pure C# SignalR Hub Client (0 External Dependencies)
```csharp
using ZeroNetwork.RealTime;

using var hub = new ZeroSignalRClient("ws://erp-server:5000/hub/inventory");

// Register server-to-client event listener
hub.On<StockUpdateDto>("OnStockChanged", dto =>
{
    // Instantly update WinForms / WPF UI
    Console.WriteLine($"[RealTime] SKU {dto.Sku}: {dto.CurrentQty}");
});

await hub.StartAsync();

// Fire-and-forget invocation
await hub.SendAsync("JoinWarehouseGroup", "WH-01");

// Request-response invocation
int pendingCount = await hub.InvokeAsync<int>("GetPendingOrdersCount", "WH-01");
```

### 4. Resilient WebSocket Client (Auto-Reconnect & Keep-Alive)
```csharp
using ZeroNetwork.RealTime;

var options = new ZeroWebSocketOptions
{
    ServerUri = new Uri("ws://edge-gateway.local:8080/telemetry"),
    AutoReconnect = true,
    InitialReconnectDelay = TimeSpan.FromSeconds(1),
    KeepAliveInterval = TimeSpan.FromSeconds(15)
};

using var ws = new ZeroWebSocketClient(options);
ws.MessageReceived += msg => Console.WriteLine($"Received: {msg}");
ws.Reconnected += () => Console.WriteLine("Connection restored!");

await ws.ConnectAsync();
await ws.SendJsonAsync(new { deviceId = "CNC-01", status = "ONLINE" });
```

### 5. Micro HTTP Server with Prometheus Metrics
```csharp
using ZeroNetwork.Http;

using var server = new ZeroHttpServer(port: 9090, ipAddress: "0.0.0.0");

server.MapGet("/api/status", req => ZeroHttpResponse.Json("{\"status\":\"OK\"}"));
server.MapPost("/api/trigger", req => ZeroHttpResponse.Text("Triggered: " + req.Body));

server.Start();
// Automatic Prometheus endpoint available at: http://localhost:9090/metrics
// Health check available at: http://localhost:9090/health
```

---

## 💻 Supported Platforms

| Framework | Target Support | Dependency Footprint |
| :--- | :--- | :--- |
| **.NET 8.0+** | Native (`net8.0`) | 0 Dependencies |
| **.NET Framework** | Legacy WinForms / WPF (`net462`) | 0 Dependencies (BCL only) |
| **.NET Standard** | Universal Cross-Platform (`netstandard2.0`) | 0 Dependencies |

---

## 📄 License
MIT License © 2026 Phong Võ (`kzxl`). Part of the **ZeroPlatform** sovereign ecosystem.
