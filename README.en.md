# NewLife.Melsec - Mitsubishi PLC

> **🌐 Language**
>
> [🇨🇳 简体中文](README.zh-CN.md) · [🇺🇸 **English**](README.en.md) · [🇯🇵 日本語](README.ja.md) · [🇰🇷 한국어](README.ko.md) · [🇩🇪 Deutsch](README.de.md) · [🇫🇷 Français](README.fr.md) · [🇪🇸 Español](README.es.md) · [🇧🇷 Português](README.pt-BR.md) · [🇷🇺 Русский](README.ru.md) · [🇮🇹 Italiano](README.it.md)

---

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/NewLife.Melsec?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/NewLife.Melsec?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Melsec?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Melsec?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/NewLife.Melsec?label=dev%20nuget&logo=nuget)

Mitsubishi PLC Communication Library for .NET  

Source: https://github.com/NewLifeX/NewLife.Melsec  
NuGet: `NewLife.Melsec`  

## Introduction

**NewLife.Melsec** is a pure managed communication library for Mitsubishi PLC, built on the **NewLife.IoT** standard interface specification. It covers three major protocol families — FxLinks serial protocol, MC Ethernet protocol (3E/1E/4E frames), and SLMP protocol — with TCP/UDP/Serial multi-transport support. The library is MIT licensed, has zero dependency on commercial components, and supports Windows/Linux/macOS including ARM devices like Raspberry Pi.

Through the unified `IDriver` interface, you can integrate with **ZeroIoT/IoTEdge** gateway platforms with a single line of code, or directly call the underlying protocol stack from console or service applications.

## Key Features

- **Complete Protocol Coverage**: Full implementation of FxLinks serial (BR/WR/BW/WW), MC Ethernet 3E/1E/4E frames (Binary + ASCII), SLMP 3C frames — covering Mitsubishi FX/Q/iQ-R/L/A/FX5U series
- **Pure Managed**: No dependency on HslCommunication or other commercial libraries, only .NET standard libraries and the NewLife ecosystem
- **IoT Ecosystem Integration**: Compliant with NewLife.IoT v2.6+ `IDriver`/`IDriverParameter`/`INode` specifications, plug-and-play
- **Batch Read Optimization**: `BuildSegments` algorithm automatically merges adjacent addresses, reducing communication rounds and dramatically improving collection efficiency
- **Multi-Station Support**: Multiple PLCs can share a single serial port connection, with reference counting for lifecycle management
- **Data Type Conversion**: `ConvertToWords` supports Boolean/Int16/Int32/Float/Double → UInt16[], eliminating manual encoding for write operations
- **Advanced Protocol Features**: 4E frame (serial number validation), random read (0403h), remote RUN/STOP, remote password lock, monitor registration, UDP/Serial multi-transport modes
- **APM Tracing**: Built-in `ITracer` instrumentation, seamlessly integrates with the Stardust distributed platform for troubleshooting
- **Cross-Platform Multi-Target**: `net10.0/net9.0/net8.0/net7.0/net6.0/netstandard2.1/netstandard2.0/net461/net45` — from .NET Framework 4.5 to .NET 10

## Documentation Index

| Document | Description |
|----------|-------------|
| [Requirements](Doc/需求文档.md) | Vision, core objectives, feature requirements, backlog & scope |
| [Feature List](Doc/功能清单.md) | 3D tracking (implementation/test/comment) with parent requirement completion |
| [Architecture](Doc/架构设计.md) | Layered architecture, core components, key flows, design decisions |
| [Competitive Analysis](Doc/竞品分析.md) | Feature comparison matrix & gap analysis |

## Protocol Support

| Protocol | Transport | PLC Series | Status |
|:--------:|:---------:|:-----------|:------:|
| FxLinks (Computer Link) | RS-485 Serial | FX Series (FX1S/FX1N/FX2N/FX3U etc.) | ✅ Done |
| MC 3E Frame (Binary/ASCII) | TCP/IP Ethernet | Q/iQ-R/L Series, FX3U-ENET, FX5U | ✅ Done |
| MC 1E Frame | TCP/IP Ethernet | A Series (legacy compatible) | ✅ Done |
| MC 4E Frame | TCP/IP Ethernet | iQ-R Series (extended frame with serial number) | ✅ Done |
| MC (UDP/Serial Mode) | UDP / Serial | Multi-transport support | ✅ Done |
| SLMP (3C Frame) | TCP/IP Ethernet | iQ-R/iQ-F Series (latest standard) | ✅ Done |

## FxLinks Protocol
Common FxLinks commands:
1. ENQ (05) request, BR bit read, WR word read, BW bit write, WW word write, terminated by checksum
2. STX (02) read response, including bit read and word read data, terminated by ETX (03) and checksum
3. ACK (06) write confirmation response
4. NAK (15) write failure response with error code

## Quick Start

### Installation

```
dotnet add package NewLife.Melsec
```

### FxLinks Serial Communication (FX Series)

Default serial parameters: Baud rate 9600, data bits 7, even parity, 1 stop bit (per Mitsubishi computer link specification).

```csharp
using NewLife.Melsec.Protocols;

// Direct protocol layer usage
using var fx = new FxLinks
{
    PortName = "COM3",
    Baudrate = 9600,    // FxLinks default
    DataBits = 7,
    Parity   = System.IO.Ports.Parity.Even,
};
fx.Open();

Byte host = 1;  // Station number

// Read 4 words from data register D100
UInt16[] words = fx.ReadWord(host, "D100", 4);
Console.WriteLine($"D100={words[0]}, D101={words[1]}");

// Read 8 bits from auxiliary relay M0
Byte[] bits = fx.ReadBit(host, "M0", 8);
Console.WriteLine($"M0={bits[0]}, M1={bits[1]}");

// Write bit (Y0 = ON)
fx.WriteBit(host, "Y0", new Byte[] { 1 });

// Write word (D210 = 1234)
fx.WriteWord(host, "D210", new UInt16[] { 1234 });
```

### FxLinks Driver Mode (IoTEdge Integration)

```csharp
using NewLife.Melsec.Drivers;
using NewLife.IoT.Drivers;

var driver    = new FxLinksDriver();
var parameter = new FxLinksParameter
{
    PortName   = "COM3",
    Baudrate   = 9600,
    Host       = 1,
    BatchSize  = 64,   // Max points per batch
    BatchStep  = 2,    // Merge points with address diff ≤ 2
    BatchDelay = 10,   // Delay between consecutive batches (ms)
};

INode node = driver.Open(device, parameter);

// Batch read (auto-merges adjacent addresses)
IDictionary<IPoint, Object> values = driver.Read(node, points);

driver.Close(node);
```

### MC Ethernet Communication (Q/iQ-R/FX5U Series)

```csharp
using NewLife.Melsec.Protocols;

// Using protocol layer
using var mc = new MCProtocol
{
    Address = "192.168.1.10:6000",
    Timeout = 5000,
};
mc.Open();

// Read 10 words from D100
UInt16[] words = mc.ReadWords(DeviceCode.D, 100, 10);
Console.WriteLine($"D100={words[0]}, D101={words[1]}");

// Read 16 bits from M200
Boolean[] bits = mc.ReadBits(DeviceCode.M, 200, 16);

// Write D100 = 1234
mc.WriteWords(DeviceCode.D, 100, [1234]);
```

### MC Driver Mode (IoTEdge Integration)

```csharp
using NewLife.Melsec.Drivers;
using NewLife.IoT.Drivers;

var driver = new MCDriver();
var parameter = new MCParameter
{
    Address   = "192.168.1.10:6000",
    Timeout   = 5000,
    BatchSize = 256,
};

INode node = driver.Open(device, parameter);

// Batch read (identical interface to FxLinksDriver)
IDictionary<IPoint, Object> values = driver.Read(node, points);

// Write word register
driver.Write(node, new ThingPoint { Address = "D100" }, (Int16)123);

driver.Close(node);
```

### Address Format Reference

| Device | Format | Example | Description |
|--------|--------|---------|-------------|
| Data Register | D{decimal} | D100, D210 | Word device |
| Auxiliary Relay | M{decimal} | M0, M103 | Bit device |
| Input Relay | X{octal} | X0, X17 | Bit device, octal |
| Output Relay | Y{octal} | Y0, Y17 | Bit device, octal |
| Timer | T{decimal} | T0, T10 | Word/Bit device |
| Counter | C{decimal} | C0, C200 | Word/Bit device |

## FAQ

**Q: Why does FxLinks read return null?**  
A: Usually caused by serial port parameter mismatch (baud rate/parity) or communication timeout. Verify that the PLC's computer link function is enabled and matches the driver parameters.

**Q: How to adjust BatchSize / BatchStep / BatchDelay?**  
A: `BatchStep=1` merges addresses with difference ≤ 1; `BatchSize=0` disables batch size limit; `BatchDelay=10` adds 10ms delay between batches to reduce PLC load.

**Q: What transport modes does MC protocol support?**  
A: TCP (default), UDP, and Serial modes, switchable via `MCParameter.TransportMode`. Frame types include 3E (Qna compatible), 1E (A Series), and 4E (iQ-R Series).

**Q: How to integrate with IoTEdge gateway?**  
A: Driver registration names are `"MelsecFxLinks"` (FxLinks) and `"MelsecMC"` (MC protocol). Select the driver on the IoTEdge gateway platform and configure parameters via XML.

## Ecosystem Map

NewLife is a layered ecosystem of independently versioned open-source projects. The map below is shared verbatim by every NewLife repository; each project links to its repository and its primary NuGet package.

| Project | What it does | Downloads |
| --- | --- | --- |
| **Foundation** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | Logging, configuration, caching, networking, serialization, scheduling and APM tracing. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | Data middleware: single tables at the tens-of-billions scale, automatic sharding, read-write splitting, 10+ databases. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | High-performance network library: 1.77M pps / 14.5 Gbps on a single machine (TCP 1KB, 2026-10), 4M long-lived TCP connections. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | Protocol communication and RPC framework for client/server applications and IoT devices. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | Installs .NET applications as OS daemons: Windows services and Linux systemd. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | `dotnet new` project templates for console, web, API, service, data and IoT projects. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **Data & Messaging** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Redis client with microsecond latency, millions of ops/s and built-in message queues. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Pure managed RocketMQ client for Apache RocketMQ and Alibaba Cloud Message Queue. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT client and server, including Alibaba Cloud IoT connectivity. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | Hybrid database in C#: embedded or networked, relational, time-series, message queue and KV in one. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | MySql driver and helper layer built on the NewLife data stack. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web, AI & Distributed** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | Rapid admin-platform framework: permissions, SSO, OAuth server and CRUD scaffolding. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | Distributed service platform: node management, APM, configuration center, registry and release center. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | Distributed task and real-time data scheduling platform with sharding, retries and backfill. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | Unified client for 46 LLM providers plus ChatAI, a ready-to-run web chat application. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **Industrial & IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | IoT standard library defining protocol contracts and device abstractions. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Modbus TCP/RTU/ASCII implementation on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | Siemens PLC protocol on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | Omron PLC protocol on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | Mitsubishi Melsec PLC protocol on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | OPC client for industrial data access. | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet building-automation protocol stack. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | Lightweight open-source IoT platform with gateway and device management. | — |
| **Vehicle Telematics** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | JT/T 808 vehicle positioning protocol: 70+ message bodies, 2011/2013/2019. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | JT/T 809 platform-to-platform protocol for transport authorities. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | JT/T 1078 in-vehicle video: RTP parsing, FLV/fMP4 remuxing and live streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **Domain Components** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | One package for Excel, Word, PPT, PDF and 15+ document formats. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | Cross-platform audio: codecs, device I/O, DSP, containers and streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | Video codecs, container formats, frame processing and streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | Unified interface over Baidu, AMap, Tencent and Tianditu with geocoding and coordinate conversion. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4 geolocation with an offline memory-mapped database and an online API fallback. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **Tools & Products** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | Developer toolbox: network, serial port, crypto, regex, Modbus and MQTT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | Self-hosted Git source code management system. | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | Installs the matching .NET runtime automatically when launched from an older host. | — |

> Badges show the primary package of each project; theme, extension and satellite packages are listed on their NuGet pages. The 30+ packages not listed here (FTP, LoRa, Office extensions, protocol drivers and more) are available under the [NewLifeX organization](https://github.com/NewLifeX) and on [newlifex.com](https://newlifex.com).

## Team

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

Founded in 2002, the NewLife team has spent more than two decades on the .NET and IoT ecosystem, maintains nearly 100 open-source projects (34 of them core to this map), and delivers integrated software and hardware solutions for power, logistics, healthcare, industrial control and internet industries. We live by the motto "learning has no seniority — mastery earns respect".

Open-source components have been downloaded <!--nuget-total:start-->6.48M<!--nuget-total:end--> times from NuGet (ecosystem-wide, as of 2026-10).

Website: https://newlifex.com · Open source organization: https://github.com/NewLifeX

<details>
<summary>WeChat Official Account: 智能大石头 (scan to follow)</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>
