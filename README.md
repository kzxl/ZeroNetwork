# ZeroNetwork

High-performance, cross-platform network infrastructure and diagnostics library for the ZeroPlatform ecosystem.

## Features
- **Network Discovery**: Accurate local IPv4/IPv6 address retrieval with advanced virtual adapter filtering (Hyper-V, VMware, WSL, VirtualBox, Docker, VPN).
- **Physical MAC Address**: Retrieve clean, formatted physical MAC address for hardware machine licensing and audit trails.
- **Network Diagnostics**: Non-blocking asynchronous TCP port probe (`NetworkProbe.IsPortOpenAsync`) and ping utilities.
- **Network State Watcher**: Real-time network availability and address change monitoring with built-in debounce protection.

## Platforms
- `.NET Standard 2.0`
- `.NET Framework 4.6.2`
- `.NET 8.0+`
