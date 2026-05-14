# NewLife.Melsec - 三菱PLC

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/NewLife.Melsec?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/NewLife.Melsec?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Melsec?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Melsec?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/NewLife.Melsec?label=dev%20nuget&logo=nuget)

三菱PLC协议  

源码： https://github.com/NewLifeX/NewLife.Melsec  
Nuget：NewLife.Melsec  

## 项目简介

NewLife.Melsec 是基于 **NewLife.IoT** 标准接口规范的三菱 PLC 纯托管通信库，覆盖工业现场最常用的两大协议场景：RS-485 串口（FxLinks 计算机链路协议）和 TCP 以太网（MC 协议）。整个库采用 MIT 开源协议，不依赖任何商业授权组件，支持 Windows/Linux/macOS，可在 Raspberry Pi 等 ARM 边缘设备上运行。

通过统一的 `IDriver` 接口，可一行代码接入 **ZeroIoT/IoTEdge** 网关平台，也可直接在控制台或服务程序中调用底层协议栈。

## 主要特点

- **协议完整**：完整实现 FxLinks 串口协议（BR/WR/BW/WW 四指令），MC 以太网协议（3E/1E 帧）规划开发中
- **纯托管实现**：不依赖 HslCommunication 等商业库，仅基于 .NET 标准类库和 NewLife 生态
- **IoT 生态集成**：符合 NewLife.IoT v2.6+ 的 `IDriver`/`IDriverParameter`/`INode` 规范，即插即用
- **批量读取优化**：`BuildSegments` 算法自动合并相邻地址，减少通信次数，大幅提升采集效率
- **多站号支持**：同一串口可连接多台 PLC，驱动层用引用计数管理共享连接生命周期
- **APM 链路追踪**：内置 `ITracer` 埋点，与星尘分布式平台无缝集成，方便排查通信问题
- **跨平台多目标**：`netstandard2.1 / netstandard2.0 / net461`，支持从 .NET Framework 4.6 到 .NET 9

## 协议支持

| 协议 | 传输方式 | 适用 PLC 型号 | 完成状态 |
|:---:|:---:|:---|:---:|
| FxLinks（计算机链路） | RS-485 串口 | FX 系列（FX1S/FX1N/FX2N/FX3U 等） | ✅ 已完成 |
| MC 3E 帧（二进制） | TCP/IP 以太网 | Q/iQ-R/L 系列、FX3U-ENET、FX5U 等 | ⬜ 规划中 |
| MC 1E 帧 | TCP/IP 以太网 | A 系列（旧式兼容） | ⬜ 规划中 |
| SLMP（3C/4C 帧） | TCP/IP 以太网 | iQ-R/iQ-F 系列（最新标准） | ⬜ 远期规划 |

## FxLinks协议
FxLinks协议常用指令：
1. ENQ05询问，BR位读取，BW字读取，BW位写入，WW字写入，以校验和结束
2. STX02读取响应，包括位读取和字读取的响应数据，以ETX03和校验和结束
3. ACK确认写入响应，包括位写入和字写入
4. NAK不确认写入响应，包括位写入和字写入

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

**Q：MC 以太网协议何时支持？**  
A：MC 3E 帧（`MCDriver` / `MCProtocol`）正在规划实现中，会完全符合 IoT 接口规范，届时地址写法与 FxLinks 保持一致。

**Q：如何接入 IoTEdge 网关？**  
A：注册驱动名称为 `"MelsecFxLinks"`（`[Driver("MelsecFxLinks")]` 特性已标注），在 IoTEdge 网关平台选择该驱动并填写 `FxLinksParameter` XML 配置即可。

## 新生命项目矩阵
各项目默认支持net9.0/netstandard2.1/netstandard2.0/net4.62/net4.5，旧版（2024.0801）支持net4.0/net2.0  

|                               项目                               | 年份  | 说明                                                                                        |
| :--------------------------------------------------------------: | :---: | ------------------------------------------------------------------------------------------- |
|                             基础组件                             |       | 支撑其它中间件以及产品项目                                                                  |
|          [NewLife.Core](https://github.com/NewLifeX/X)           | 2002  | 核心库，日志、配置、缓存、网络、序列化、APM性能追踪                                         |
|    [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode)    | 2005  | 大数据中间件，单表百亿级，MySql/SQLite/SqlServer/Oracle/PostgreSql/达梦，自动分表，读写分离 |
|      [NewLife.Net](https://github.com/NewLifeX/NewLife.Net)      | 2005  | 网络库，单机千万级吞吐率（2266万tps），单机百万级连接（400万Tcp长连接）                     |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 2011  | 协议通信库，提供CS应用通信框架，支持Http/RPC通信框架，高吞吐，物联网设备低开销易接入        |
|     [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube)     | 2010  | 魔方快速开发平台，集成了用户权限、SSO登录、OAuth服务端等，单表100亿级项目验证               |
|    [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent)    | 2008  | 服务管理组件，把应用安装成为操作系统守护进程，Windows服务、Linux的Systemd                   |
|     [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero)     | 2020  | Zero零代脚手架，基于NewLife组件生态的项目模板NewLife.Templates，Web、WebApi、Service        |
|                              中间件                              |       | 对接知名中间件平台                                                                          |
|    [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)    | 2017  | Redis客户端，微秒级延迟，百万级吞吐，丰富的消息队列，百亿级数据量项目验证                   |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018  | RocketMQ纯托管客户端，支持Apache RocketMQ和阿里云消息队列，十亿级项目验                     |
|     [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT)     | 2019  | 物联网消息协议，MqttClient/MqttServer，客户端支持阿里云物联网                               |
|      [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT)      | 2022  | IoT标准库，定义物联网领域的各种通信协议标准规范                                             |
|   [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus)   | 2022  | ModbusTcp/ModbusRTU/ModbusASCII，基于IoT标准库实现，支持ZeroIoT平台和IoTEdge网关            |
|  [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens)  | 2022  | 西门子PLC协议，基于IoT标准库实现，支持IoT平台和IoTEdge                                      |
|      [NewLife.Map](https://github.com/NewLifeX/NewLife.Map)      | 2022  | 地图组件库，封装百度地图、高德地图、腾讯地图、天地图                                        |
|    [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio)    | 2023  | 音频编解码库，PCM/ADPCMA/G711A/G722U/WAV/AAC                                                |
|                             产品平台                             |       | 产品平台级，编译部署即用，个性化自定义                                                      |
|         [Stardust](https://github.com/NewLifeX/Stardust)         | 2018  | 星尘，分布式服务平台，节点管理、APM监控中心、配置中心、注册中心、发布中心                   |
|           [AntJob](https://github.com/NewLifeX/AntJob)           | 2019  | 蚂蚁调度，分布式大数据计算平台（实时/离线），蚂蚁搬家分片思想，万亿级数据量项目验证         |
|      [NewLife.ERP](https://github.com/NewLifeX/NewLife.ERP)      | 2021  | 企业ERP，产品管理、客户管理、销售管理、供应商管理                                           |
|         [CrazyCoder](https://github.com/NewLifeX/XCoder)         | 2006  | 码神工具，众多开发者工具，网络、串口、加解密、正则表达式、Modbus、MQTT                      |
|           [EasyIO](https://github.com/NewLifeX/EasyIO)           | 2023  | 简易文件存储，支持分布式系统中文件集中存储。                                                |
|           [XProxy](https://github.com/NewLifeX/XProxy)           | 2005  | 产品级反向代理，NAT代理、Http代理                                                           |
|        [HttpMeter](https://github.com/NewLifeX/HttpMeter)        | 2022  | Http压力测试工具                                                                            |
|         [GitCandy](https://github.com/NewLifeX/GitCandy)         | 2015  | Git源代码管理系统                                                                           |
|          [SmartOS](https://github.com/NewLifeX/SmartOS)          | 2014  | 嵌入式操作系统，完全独立自主，支持ARM Cortex-M芯片架构                                      |
|          [SmartA2](https://github.com/NewLifeX/SmartA2)          | 2019  | 嵌入式工业计算机，物联网边缘网关，高性能.NET8主机，应用于工业、农业、交通、医疗             |
|                          FIoT物联网平台                          | 2020  | 物联网整体解决方案，建筑、环保、农业，软硬件及大数据分析一体化，单机十万级点位项目验证      |
|                        UWB高精度室内定位                         | 2020  | 厘米级（10~20cm）高精度室内定位，软硬件一体化，与其它系统联动，大型展厅项目验证             |



## 新生命开发团队
![XCode](https://newlifex.com/logo.png)  

新生命团队（NewLife）成立于2002年，是新时代物联网行业解决方案提供者，致力于提供软硬件应用方案咨询、系统架构规划与开发服务。  
团队主导的80多个开源项目已被广泛应用于各行业，Nuget累计下载量高达400余万次。  
团队开发的大数据中间件NewLife.XCode、蚂蚁调度计算平台AntJob、星尘分布式平台Stardust、缓存队列组件NewLife.Redis以及物联网平台FIoT，均成功应用于电力、高校、互联网、电信、交通、物流、工控、医疗、文博等行业，为客户提供了大量先进、可靠、安全、高质量、易扩展的产品和系统集成服务。  

我们将不断通过服务的持续改进，成为客户长期信赖的合作伙伴，通过不断的创新和发展，成为国内优秀的IoT服务供应商。  

`新生命团队始于2002年，部分开源项目具有20年以上漫长历史，源码库保留有2010年以来所有修改记录`  
网站：https://newlifex.com  
开源：https://github.com/newlifex  
QQ群：1600800/1600838  
微信公众号：  
![智能大石头](https://newlifex.com/stone.jpg)  
