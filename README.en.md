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

## NewLife Project Matrix

| Project | Year | Description |
| :--- | :--- | :--- |
| [NewLife.Core](https://github.com/NewLifeX/X) | 2002 | Core library: logging, config, cache, networking, serialization, APM |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 2005 | Big data middleware, 10B+ rows per table |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 2005 | Network library, 10M+ TPS throughput |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 2010 | Rapid development platform |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | 2017 | Redis client |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | 2022 | IoT standard library |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | 2022 | Modbus protocol |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | 2022 | Siemens PLC protocol |
| [Stardust](https://github.com/NewLifeX/Stardust) | 2018 | Distributed service platform |
| [AntJob](https://github.com/NewLifeX/AntJob) | 2019 | Distributed computing platform |

## NewLife Team

The NewLife team was founded in 2002, dedicated to IoT solution innovation. We have published over 80 open-source projects with 4M+ NuGet downloads across various industries including power, education, Internet, telecom, transportation, logistics, industrial control, healthcare, and cultural heritage.

Website: https://newlifex.com  
GitHub: https://github.com/newlifex  
QQ Group: 1600800/1600838
