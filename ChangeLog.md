# NewLife.Melsec 版本更新记录

## v1.3.2026.1024 (2026-10-24)

### 依赖升级
- **NewLife.Core 12.0**：升级到 12.0.2026.1024，适配 `Packet` 类型移除，原视图语义改由 `ArrayPacket` 承接
- **NewLife.IoT 3.2**：升级到 3.2.2026.1024，驱动接口保持兼容

### 工程与构建
- **解决方案格式**：`NewLife.Melsec.sln` 迁移为新版 `.slnx` 解决方案格式
- **CI 发布**：统一使用 Ubuntu runner；正式发版同时发布 GitHub Packages 与 NuGet.org，并根据 ChangeLog 自动创建 GitHub Release（附 nupkg/snupkg）
- **CI 测试**：解决方案编译与 net8.0 单元测试独立执行，测试结果以 trx 归档

### 文档
- **生态版图与团队**：全部语言版本 README 的生态版图、团队介绍与 NewLife.Core 保持一致
- **架构设计**：补充 NewLife.Core 12 升级与 `IPacket` 迁移决策

### 兼容性
- **破坏性变更**：`FxLinksMessage.ToPacket()` / `FxLinksResponse.ToPacket()` 返回类型由已移除的 `Packet` 改为 `IPacket`

---

## v1.2.2026.0902 (2026-09-02)

### 新增功能
- **MC 协议帧扩展**：新增 1E 帧（A 系列兼容）、4E 帧（iQ-R 系列，子头 5400h/D400h 含序列号）、3E 帧 ASCII 十六进制模式
- **MC 传输模式扩展**：新增串口协议（1C/3C/4C）与 UDP 传输支持
- **SLMP 协议**：新增 SLMP 3C 帧支持（`SLMPMessage`/`SLMPResponse`）
- **MC 高级功能**：随机读取（0403h）、远程 PLC 启停（RUN/STOP）、远程密码锁定（1630h/1631h）、监视注册（0801h）
- **数据类型转换**：新增多类型数据转换读取（ReadInt32/ReadUInt32/ReadSingle/ReadDouble/ReadString）
- **标签名访问**：`MCDriver.TagMap` 点位名→地址映射字典

### 功能优化
- **驱动增强**：驱动接口异步化与批量操作，统一读写结果类型
- **依赖升级**：NewLife.Core 升级到 11.19.2026.901，NewLife.IoT 升级到 3.1.2026.801
- **测试覆盖**：216 个单元测试全部通过，覆盖全部 34 个功能点
- **国际化**：csproj 国际化 + 10 语言 README

---

## v1.2.2026.0502 (2026-05-02)

### 功能优化
- **IoT 架构升级**：升级到 v2.6 的 IoT 架构，提升系统稳定性和性能

---
