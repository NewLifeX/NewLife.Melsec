# NewLife.Melsec - 三菱PLC / Mitsubishi PLC

> **🌐 选择语言 / Language**
>
> [🇨🇳 简体中文](README.zh-CN.md) · [🇺🇸 English](README.en.md) · [🇯🇵 日本語](README.ja.md) · [🇰🇷 한국어](README.ko.md) · [🇩🇪 Deutsch](README.de.md) · [🇫🇷 Français](README.fr.md) · [🇪🇸 Español](README.es.md) · [🇧🇷 Português](README.pt-BR.md) · [🇷🇺 Русский](README.ru.md) · [🇮🇹 Italiano](README.it.md)

---

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/NewLife.Melsec?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/NewLife.Melsec?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Melsec?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Melsec?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/NewLife.Melsec?label=dev%20nuget&logo=nuget)

Mitsubishi PLC communication library for .NET — Pure managed, MIT licensed, IoT-ready.

Source: https://github.com/NewLifeX/NewLife.Melsec  
NuGet: `NewLife.Melsec`  
Docs: [Doc/](Doc/)

| Language | File | Language | File |
|----------|------|----------|------|
| 🇨🇳 **简体中文** | [README.zh-CN.md](README.zh-CN.md) | 🇺🇸 **English** | [README.en.md](README.en.md) |
| 🇯🇵 **日本語** | [README.ja.md](README.ja.md) | 🇰🇷 **한국어** | [README.ko.md](README.ko.md) |
| 🇩🇪 **Deutsch** | [README.de.md](README.de.md) | 🇫🇷 **Français** | [README.fr.md](README.fr.md) |
| 🇪🇸 **Español** | [README.es.md](README.es.md) | 🇧🇷 **Português** | [README.pt-BR.md](README.pt-BR.md) |
| 🇷🇺 **Русский** | [README.ru.md](README.ru.md) | 🇮🇹 **Italiano** | [README.it.md](README.it.md) |

---

## 📚 Project Documents

| Document | Description |
|----------|-------------|
| [Requirements](Doc/需求文档.md) | Vision, goals, features, scope |
| [Feature List](Doc/功能清单.md) | 3D tracking (implementation/test/comment) |
| [Architecture](Doc/架构设计.md) | Layered architecture, components, design decisions |
| [Competitive Analysis](Doc/竞品分析.md) | Comparison matrix & gap analysis |

## 🌟 Quick Overview

**NewLife.Melsec** is a pure managed communication library for Mitsubishi PLC, built on the **NewLife.IoT** standard interface specification. It covers three major protocol families — FxLinks serial, MC Ethernet (3E/1E/4E frames), and SLMP — with TCP/UDP/Serial multi-transport support.

| Protocol | Transport | PLC Series | Status |
|:--------:|:---------:|:-----------|:------:|
| FxLinks | RS-485 Serial | FX Series | ✅ Done |
| MC 3E (Binary/ASCII) | TCP/IP | Q, iQ-R, L, FX5U | ✅ Done |
| MC 1E | TCP/IP | A Series (legacy) | ✅ Done |
| MC 4E | TCP/IP | iQ-R (extended frame) | ✅ Done |
| MC (UDP/Serial) | UDP / Serial | Multi-transport | ✅ Done |
| SLMP 3C | TCP/IP | iQ-R, iQ-F | ✅ Done |

**Key features**: Pure managed, MIT license · `IDriver` standard interface · Batch read optimization · Multi-station shared connections · Data type auto-conversion · APM tracing · 9 target frameworks (net45~net10.0)

---

## 📋 新生命项目矩阵 / NewLife Project Matrix

For the full project ecosystem overview and team information, please refer to the language-specific README files listed above.  
