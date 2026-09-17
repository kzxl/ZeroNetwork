# ZeroNetwork

High-performance, zero-dependency cross-platform network infrastructure, topology discovery, diagnostics, and embedded micro-services library for the ZeroPlatform ecosystem.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET Multi-Targeting](https://img.shields.io/badge/.NET-8.0%20%7C%204.6.2%20%7C%20Standard%202.0-purple.svg)](https://dotnet.microsoft.com/)
[![Dependencies: 0](https://img.shields.io/badge/Dependencies-0%20(Pure%20C%23)-brightgreen.svg)]()

---

## 🌟 Key Capabilities

### 1. Topology & Hardware Discovery
- **`IPNetwork`**: High-performance, allocation-free IPv4/IPv6 CIDR arithmetic (`Parse`, `Contains`, `EnumerateUsableHosts`, `BroadcastAddress`, `Netmask`).
- **`MacAddress`**: Blittable 6-byte struct with IEEE 24-bit OUI vendor identification (VMware, Hyper-V, VirtualBox, Docker, QEMU/KVM, Xen) and zero-allocation span formatting.
- **`VirtualAdapterFilter`**: Hybrid keyword heuristics and hardware OUI vendor filtering with extensible user whitelist rules.
- **`NetworkInfo`**: Deterministic physical NIC selection, multi-homed adapter enumeration, primary subnet calculation, and Default Gateway extraction.

### 2. Layer 2 & Industrial Device Discovery
- **`ArpTable`**: Fast IP $\leftrightarrow$ MAC address resolution via `IpHlpApi.dll` (`SendARP` / `GetIpNetTable`) on Windows and `/proc/net/arp` on Linux.
- **`WakeOnLan`**: Standard 102-byte Magic Packet generation and subnet-directed UDP broadcasting for remote IPC/node power-on.
- **`SsdpDiscovery`**: Simple Service Discovery Protocol (UPnP) client for zero-configuration camera, printer, and smart device discovery.

### 3. High-Throughput Network Diagnostics
- **`NetworkProbe`**: Hardened non-blocking TCP port probing (`IsPortOpenAsync`) with immediate cancellation/timeout socket abort (zero socket leaks).
- **`PortScanner`**: Parallel bounded-concurrency TCP port and subnet scanner (`ScanPortRangeAsync`, `ScanSubnetAsync`).
- **`Traceroute`**: Hop-by-hop ICMP route tracer with per-hop roundtrip latency measurement.
- **`PathMtuDiscovery`**: Binary search PMTU discovery with Don't Fragment (DF) ICMP packets (validating Jumbo Frames MTU 9000 for GigE Vision cameras).
- **`ConnectivityHeartbeat`**: Active multi-target WAN/LAN reachability monitor.

### 4. Sockets, Micro-Services & High-Performance HTTP Client
- **`ZeroApiClient`**: Ultra-high-throughput stream-based HTTP API Client. Eliminates LOH fragmentation via direct stream serialization/deserialization, automatic GZip/Brotli socket decompression, pooled connection lifetime management, and exponential backoff retry.
- **`ZeroHttpExtensions`**: Fluent string extensions (`url.GetJsonAsync<T>()`, `url.PostJsonAsync<T>()`) providing zero-allocation drop-in replacements for legacy HTTP extensions.
- **`UdpMulticastClient`**: IGMPv2/v3 multicast group publisher/subscriber with physical interface binding.
- **`ZeroHttpServer`**: Sovereign, non-admin pure C# micro HTTP/1.1 server for Edge REST APIs and built-in Prometheus `/metrics` telemetry.

---

## 💻 Supported Platforms
- `.NET 8.0+`
- `.NET Framework 4.6.2+`
- `.NET Standard 2.0`

## 📄 License
MIT License © 2026 Phong Võ (`kzxl`). Part of the **ZeroPlatform** sovereign ecosystem.
