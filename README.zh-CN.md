# NewLife.Melsec - 三菱PLC

> **🌐 选择语言**
>
> [🇨🇳 **简体中文**](README.zh-CN.md) · [🇺🇸 English](README.en.md) · [🇯🇵 日本語](README.ja.md) · [🇰🇷 한국어](README.ko.md) · [🇩🇪 Deutsch](README.de.md) · [🇫🇷 Français](README.fr.md) · [🇪🇸 Español](README.es.md) · [🇧🇷 Português](README.pt-BR.md) · [🇷🇺 Русский](README.ru.md) · [🇮🇹 Italiano](README.it.md)

---

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/NewLife.Melsec?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/NewLife.Melsec?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Melsec?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Melsec?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/NewLife.Melsec?label=dev%20nuget&logo=nuget)

三菱PLC协议  

源码： https://github.com/NewLifeX/NewLife.Melsec  
Nuget：NewLife.Melsec  

## 项目简介

NewLife.Melsec 是基于 **NewLife.IoT** 标准接口规范的三菱 PLC 纯托管通信库，覆盖 FxLinks 串口协议、MC 以太网协议（3E/1E/4E 帧）和 SLMP 协议三大协议场景，支持 TCP/UDP/串口多传输模式。整个库采用 MIT 开源协议，不依赖任何商业授权组件，支持 Windows/Linux/macOS，可在 Raspberry Pi 等 ARM 边缘设备上运行。

通过统一的 `IDriver` 接口，可一行代码接入 **ZeroIoT/IoTEdge** 网关平台，也可直接在控制台或服务程序中调用底层协议栈。

## 主要特点

- **协议完整**：完整实现 FxLinks 串口协议（BR/WR/BW/WW 四指令）、MC 以太网 3E/1E/4E 帧（二进制+ASCII 模式）、SLMP 3C 帧，覆盖三菱 FX/Q/iQ-R/L/A/FX5U 全系列
- **纯托管实现**：不依赖 HslCommunication 等商业库，仅基于 .NET 标准类库和 NewLife 生态
- **IoT 生态集成**：符合 NewLife.IoT v2.6+ 的 `IDriver`/`IDriverParameter`/`INode` 规范，即插即用
- **批量读取优化**：`BuildSegments` 算法自动合并相邻地址，减少通信次数，大幅提升采集效率
- **多站号支持**：同一串口可连接多台 PLC，驱动层用引用计数管理共享连接生命周期
- **数据类型转换**：`ConvertToWords` 支持 Boolean/Int16/Int32/Float/Double → UInt16[]，写操作无需手动编码
- **高级协议功能**：支持 4E 帧（序列号校验）、随机读取（0403h）、远程 PLC 启停（RUN/STOP）、远程密码锁定、监视注册、UDP/串口多传输模式
- **APM 链路追踪**：内置 `ITracer` 埋点，与星尘分布式平台无缝集成，方便排查通信问题
- **跨平台多目标**：`net10.0/net9.0/net8.0/net7.0/net6.0/netstandard2.1/netstandard2.0/net461/net45`，支持从 .NET Framework 4.5 到 .NET 10

## 文档索引

| 文档 | 说明 |
|------|------|
| [需求文档](Doc/需求文档.md) | 愿景、核心目标、功能需求、暂缓清单与边界 |
| [功能清单](Doc/功能清单.md) | 功能点三维追踪（实现/测试/注释）及父需求完成度 |
| [架构设计](Doc/架构设计.md) | 分层架构、核心组件、关键流程、设计决策 |
| [竞品分析](Doc/竞品分析.md) | 主流竞品功能对比矩阵与差距分析 |

## 协议支持

| 协议 | 传输方式 | 适用 PLC 型号 | 完成状态 |
|:---:|:---:|:---|:---:|
| FxLinks（计算机链路） | RS-485 串口 | FX 系列（FX1S/FX1N/FX2N/FX3U 等） | ✅ 已完成 |
| MC 3E 帧（二进制/ASCII） | TCP/IP 以太网 | Q/iQ-R/L 系列、FX3U-ENET、FX5U 等 | ✅ 已完成 |
| MC 1E 帧 | TCP/IP 以太网 | A 系列（旧式兼容） | ✅ 已完成 |
| MC 4E 帧 | TCP/IP 以太网 | iQ-R 系列（最新扩展帧，含序列号） | ✅ 已完成 |
| MC（UDP/串口模式） | UDP / 串口 | 支持多种传输方式 | ✅ 已完成 |
| SLMP（3C 帧） | TCP/IP 以太网 | iQ-R/iQ-F 系列（最新标准） | ✅ 已完成 |

## FxLinks 协议
FxLinks 协议常用指令：
1. ENQ（05）询问，BR 位读取、WR 字读取、BW 位写入、WW 字写入，以校验和结束
2. STX（02）读取响应，包括位读取和字读取的响应数据，以 ETX（03）和校验和结束
3. ACK（06）确认写入响应，包括位写入和字写入
4. NAK（15）不确认写入响应，包括位写入和字写入

## 快速入门

### 安装

```
dotnet add package NewLife.Melsec
```

### FxLinks 串口通信（FX 系列）

FxLinks 默认串口参数：波特率 9600、数据位 7、偶校验、1 停止位（符合三菱计算机链路规范）。

```csharp
using NewLife.Melsec.Protocols;

// 直接使用协议层
using var fx = new FxLinks
{
    PortName = "COM3",
    Baudrate = 9600,    // FxLinks 默认参数
    DataBits = 7,
    Parity   = System.IO.Ports.Parity.Even,
};
fx.Open();

Byte host = 1;  // 站号

// 读取数据寄存器 D100 起 4 个字
UInt16[] words = fx.ReadWord(host, "D100", 4);
Console.WriteLine($"D100={words[0]}, D101={words[1]}");

// 读取辅助继电器 M0 起 8 个位
Byte[] bits = fx.ReadBit(host, "M0", 8);
Console.WriteLine($"M0={bits[0]}, M1={bits[1]}");

// 写入位（Y0 = ON）
fx.WriteBit(host, "Y0", new Byte[] { 1 });

// 写入字（D210 = 1234）
fx.WriteWord(host, "D210", new UInt16[] { 1234 });
```

### FxLinks 驱动模式（IoTEdge 集成）

```csharp
using NewLife.Melsec.Drivers;
using NewLife.IoT.Drivers;

var driver    = new FxLinksDriver();
var parameter = new FxLinksParameter
{
    PortName   = "COM3",
    Baudrate   = 9600,
    Host       = 1,
    BatchSize  = 64,   // 每批最多 64 个点位
    BatchStep  = 2,    // 地址差 ≤ 2 时合并为一批
    BatchDelay = 10,   // 相邻批次间隔 10ms
};

INode node = driver.Open(device, parameter);

// 批量读取（自动合并相邻地址，减少通信次数）
IDictionary<IPoint, Object> values = driver.Read(node, points);

driver.Close(node);
```

### MC 以太网通信（Q/iQ-R/FX5U 系列）

```csharp
using NewLife.Melsec.Protocols;

// 使用协议层
using var mc = new MCProtocol
{
    Address = "192.168.1.10:6000",
    Timeout = 5000,
};
mc.Open();

// 读取 D100 起 10 个字
UInt16[] words = mc.ReadWords(DeviceCode.D, 100, 10);
Console.WriteLine($"D100={words[0]}, D101={words[1]}");

// 读取 M200 起 16 个位
Boolean[] bits = mc.ReadBits(DeviceCode.M, 200, 16);

// 写入 D100 = 1234
mc.WriteWords(DeviceCode.D, 100, [1234]);
```

### MC 驱动模式（IoTEdge 集成）

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

// 批量读取（与 FxLinksDriver 接口完全一致）
IDictionary<IPoint, Object> values = driver.Read(node, points);

// 写入字寄存器
driver.Write(node, new ThingPoint { Address = "D100" }, (Int16)123);

driver.Close(node);
```

### 地址格式参考

| 软元件 | 格式 | 示例 | 说明 |
|--------|------|------|------|
| 数据寄存器 | D{十进制} | D100, D210 | 字软元件 |
| 辅助继电器 | M{十进制} | M0, M103 | 位软元件 |
| 输入继电器 | X{八进制} | X0, X17 | 位软元件，八进制编号 |
| 输出继电器 | Y{八进制} | Y0, Y17 | 位软元件，八进制编号 |
| 定时器 | T{十进制} | T0, T10 | 字/位软元件 |
| 计数器 | C{十进制} | C0, C200 | 字/位软元件 |

## 常见问题

**Q：FxLinks 读取返回 null 是什么原因？**  
A：通常是串口参数不匹配（波特率/校验位）或通信超时。请确认 PLC 端已设置计算机链路功能，并与驱动参数完全一致。

**Q：BatchSize / BatchStep / BatchDelay 怎么调整？**  
A：`BatchStep=1` 表示地址差 ≤ 1 时自动合并；`BatchSize=0` 不限批次大小；`BatchDelay=10` 相邻批次间隔 10ms，有助于缓解 PLC 高频请求压力。

**Q：MC 协议支持哪些传输方式？**  
A：支持 TCP（默认）、UDP、串口三种传输模式，通过 `MCParameter.TransportMode` 切换。帧类型支持 3E 帧（Qna 兼容）、1E 帧（A 系列兼容）和 4E 帧（iQ-R 系列）。

**Q：如何接入 IoTEdge 网关？**  
A：驱动注册名称为 `"MelsecFxLinks"`（FxLinks）和 `"MelsecMC"`（MC 协议），在 IoTEdge 网关平台选择对应驱动并填写参数 XML 配置即可。

## 新生命项目矩阵

| 项目 | 年份 | 说明 |
| :--- | :--- | :--- |
| [NewLife.Core](https://github.com/NewLifeX/X) | 2002 | 核心库，日志、配置、缓存、网络、序列化、APM |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 2005 | 大数据中间件，单表百亿级 |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 2005 | 网络库，单机千万级吞吐 |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 2010 | 魔方快速开发平台 |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | 2017 | Redis 客户端 |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | 2022 | IoT 标准库 |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | 2022 | Modbus 协议 |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | 2022 | 西门子 PLC 协议 |
| [Stardust](https://github.com/NewLifeX/Stardust) | 2018 | 星尘分布式服务平台 |
| [AntJob](https://github.com/NewLifeX/AntJob) | 2019 | 蚂蚁调度计算平台 |

## 新生命开发团队

新生命团队（NewLife）成立于2002年，是新时代物联网行业解决方案提供者。团队主导的80多个开源项目已被广泛应用于各行业，Nuget累计下载量高达400余万次。

网站：https://newlifex.com  
开源：https://github.com/newlifex  
QQ群：1600800/1600838
