# NewLife.Melsec - 三菱PLC / Mitsubishi PLC

> **🌐 Sprache**
>
> [🇨🇳 简体中文](README.zh-CN.md) · [🇺🇸 English](README.en.md) · [🇯🇵 日本語](README.ja.md) · [🇰🇷 한국어](README.ko.md) · [🇩🇪 **Deutsch**](README.de.md) · [🇫🇷 Français](README.fr.md) · [🇪🇸 Español](README.es.md) · [🇧🇷 Português](README.pt-BR.md) · [🇷🇺 Русский](README.ru.md) · [🇮🇹 Italiano](README.it.md)

---

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/NewLife.Melsec?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/NewLife.Melsec?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Melsec?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Melsec?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/NewLife.Melsec?label=dev%20nuget&logo=nuget)

Mitsubishi SPS-Kommunikationsbibliothek für .NET  

Quelle: https://github.com/NewLifeX/NewLife.Melsec  
NuGet: `NewLife.Melsec`  

## Übersicht

**NewLife.Melsec** ist eine rein verwaltete Kommunikationsbibliothek für Mitsubishi SPS, die auf der **NewLife.IoT**-Standardschnittstellenspezifikation basiert. Sie deckt drei wichtige Protokollfamilien ab – das FxLinks-Seriellprotokoll, das MC-Ethernet-Protokoll (3E/1E/4E-Frames) und das SLMP-Protokoll – und unterstützt TCP/UDP/Serielle Multi-Transportübertragung. Die Bibliothek steht unter der MIT-Lizenz und hat keine Abhängigkeiten von kommerziellen Komponenten.

Über die einheitliche `IDriver`-Schnittstelle kann sie mit einem einzigen Code in **ZeroIoT/IoTEdge**-Gateway-Plattformen integriert werden.

## Hauptmerkmale

- **Vollständige Protokollabdeckung**: Vollständige Implementierung von FxLinks seriell (BR/WR/BW/WW), MC Ethernet 3E/1E/4E-Frames (Binär+ASCII), SLMP 3C-Frames
- **Rein verwaltete Implementierung**: Keine Abhängigkeit von HslCommunication oder anderen kommerziellen Bibliotheken, nur .NET-Standardbibliotheken
- **IoT-Ökosystem-Integration**: Konform mit NewLife.IoT v2.6+ `IDriver`/`IDriverParameter`/`INode`-Spezifikationen
- **Batch-Leseoptimierung**: Der `BuildSegments`-Algorithmus führt benachbarte Adressen automatisch zusammen
- **Multi-Station-Unterstützung**: Mehrere SPS können eine einzige serielle Portverbindung gemeinsam nutzen
- **Datentypkonvertierung**: `ConvertToWords` unterstützt Boolean/Int16/Int32/Float/Double → UInt16[]
- **Erweiterte Protokollfunktionen**: 4E-Frames, Zufallslesen, Fern-RUN/STOP, Passwortsperre, Monitorregistrierung
- **APM-Tracing**: Verteiltes Tracing mittels `ITracer`
- **Plattformübergreifend**: 9 Ziel-Frameworks von `net45` bis `net10.0`

## Protokollunterstützung

| Protokoll | Transport | Unterstützte SPS-Serien | Status |
|:---------:|:---------:|:------------------------|:------:|
| FxLinks | RS-485 Seriell | FX-Serie | ✅ Erledigt |
| MC 3E (Binary/ASCII) | TCP/IP | Q, iQ-R, L, FX5U | ✅ Erledigt |
| MC 1E | TCP/IP | A-Serie | ✅ Erledigt |
| MC 4E | TCP/IP | iQ-R-Serie | ✅ Erledigt |
| MC (UDP/Serial) | UDP / Seriell | Multi-Transport | ✅ Erledigt |
| SLMP 3C | TCP/IP | iQ-R, iQ-F | ✅ Erledigt |

## Schnellstart

```
dotnet add package NewLife.Melsec
```

Ausführliche Codebeispiele finden Sie in der [englischen](README.en.md) oder [chinesischen](README.zh-CN.md) Version.

## Dokumentation

| Dokument | Beschreibung |
|----------|-------------|
| [Anforderungsdefinition](Doc/需求文档.md) | Vision, Ziele, funktionale Anforderungen |
| [Funktionsliste](Doc/功能清单.md) | 3D-Tracking (Implementierung/Test/Kommentar) |
| [Architektur](Doc/架构设计.md) | Schichtenarchitektur, Komponenten |
| [Wettbewerbsanalyse](Doc/竞品分析.md) | Vergleichsmatrix und Lückenanalyse |

## Häufig gestellte Fragen

**F: Warum gibt FxLinks beim Lesen null zurück?**  
A: Ursache sind inkonsistente serielle Port-Parameter (Baudrate/Parität) oder Kommunikationszeitüberschreitung.

**F: Welche Transportmodi unterstützt das MC-Protokoll?**  
A: Es werden drei Modi unterstützt: TCP (Standard), UDP und Seriell.

## Ökosystem-Karte

NewLife ist ein geschichtetes Ökosystem unabhängig versionierter Open-Source-Projekte. Die folgende Karte wird wortgleich in allen NewLife-Repositories verwendet; jedes Projekt verlinkt auf sein Repository und sein primäres NuGet-Paket.

| Projekt | Funktion | Downloads |
| --- | --- | --- |
| **Grundlage** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | Protokollierung, Konfiguration, Caching, Netzwerk, Serialisierung, Zeitplanung und APM-Tracing. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | Daten-Middleware: einzelne Tabellen in der Größenordnung von zig Milliarden Zeilen, automatisches Sharding, Lese-/Schreibtrennung, über 10 Datenbanken. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | Hochperformante Netzwerkbibliothek: 1,77 Mio. pps / 14,5 Gbps auf einer Maschine (TCP 1KB, 2026-10), 4 Mio. langlebige TCP-Verbindungen. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | Protokollkommunikation und RPC-Framework für Client/Server-Anwendungen und IoT-Geräte. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | Installiert .NET-Anwendungen als Betriebssystem-Dienste: Windows-Dienste und Linux-systemd. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | `dotnet new`-Projektvorlagen für Konsole, Web, API, Dienst, Daten und IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **Daten & Messaging** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Redis-Client mit Mikrosekunden-Latenz, Millionen Operationen pro Sekunde und integrierten Nachrichtenwarteschlangen. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Vollständig verwalteter RocketMQ-Client für Apache RocketMQ und Alibaba Cloud Message Queue. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT-Client und -Server, einschließlich Alibaba Cloud IoT-Anbindung. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | Hybriddatenbank in C#: eingebettet oder im Netzwerk, relational, Zeitreihen, Nachrichtenwarteschlange und KV in einem. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | MySql-Treiber und Hilfsschicht auf Basis des NewLife-Datenstacks. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web, KI & verteilt** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | Schnelles Framework für Administrationsplattformen: Berechtigungen, SSO, OAuth-Server und CRUD-Gerüst. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | Verteilte Dienstplattform: Knotenverwaltung, APM, Konfigurationszentrum, Registry und Release-Center. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | Plattform für verteilte Aufgaben und Echtzeit-Datenplanung mit Sharding, Wiederholungen und Backfill. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | Einheitlicher Client für 46 LLM-Anbieter plus ChatAI, eine sofort einsatzbereite Web-Chat-Anwendung. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **Industrie & IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | IoT-Standardbibliothek mit Protokollverträgen und Geräteabstraktionen. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Modbus-TCP/RTU/ASCII-Implementierung auf Basis von NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | Siemens-SPS-Protokoll auf Basis von NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | Omron-SPS-Protokoll auf Basis von NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | Mitsubishi-Melsec-SPS-Protokoll auf Basis von NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | OPC-Client für industriellen Datenzugriff. | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet-Protokollstack für die Gebäudeautomation. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | Leichtgewichtige Open-Source-IoT-Plattform mit Gateway- und Geräteverwaltung. | — |
| **Fahrzeugtelematik** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | JT/T-808-Fahrzeugortungsprotokoll: über 70 Nachrichtenkörper, 2011/2013/2019. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | JT/T-809-Plattform-zu-Plattform-Protokoll für Verkehrsbehörden. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | JT/T-1078-Fahrzeugvideo: RTP-Parsing, FLV/fMP4-Remuxing und Live-Streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **Domänenkomponenten** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | Ein Paket für Excel, Word, PPT, PDF und über 15 Dokumentformate. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | Plattformübergreifendes Audio: Codecs, Geräte-I/O, DSP, Container und Streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | Videocodecs, Containerformate, Frame-Verarbeitung und Streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | Einheitliche Schnittstelle für Baidu, AMap, Tencent und Tianditu mit Geokodierung und Koordinatenumrechnung. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4-Geolokalisierung mit einer offline speichergemappten Datenbank und einer Online-API als Fallback. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **Werkzeuge & Produkte** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | Entwickler-Toolbox: Netzwerk, serielle Schnittstelle, Krypto, Regex, Modbus und MQTT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | Selbst gehostetes Git-Quellcode-Verwaltungssystem. | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | Installiert automatisch die passende .NET-Laufzeit, wenn es von einem älteren Host gestartet wird. | — |

> Die Badges zeigen das primäre Paket jedes Projekts; Theme-, Erweiterungs- und Satellitenpakete sind auf den jeweiligen NuGet-Seiten aufgeführt. Die hier nicht aufgeführten über 30 Pakete (FTP, LoRa, Office-Erweiterungen, Protokolltreiber und mehr) sind in der [NewLifeX-Organisation](https://github.com/NewLifeX) und auf [newlifex.com](https://newlifex.com) verfügbar.

## Team

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

NewLife wurde 2002 gegründet und widmet sich seit mehr als zwei Jahrzehnten dem .NET- und IoT-Ökosystem, betreut fast 100 Open-Source-Projekte (34 davon Hauptprojekte) und liefert integrierte Hard- und Softwarelösungen für die Bereiche Energie, Logistik, Gesundheitswesen, Industrieautomation und Internet. Unser Leitspruch lautet „Lernen kennt kein Dienstalter – wer es beherrscht, wird zum Lehrmeister“.

Die Open-Source-Komponenten wurden <!--nuget-total:start-->6.48M<!--nuget-total:end--> Mal von NuGet heruntergeladen (ökosystemweit, Stand 2026-10).

Website: https://newlifex.com · Open-Source-Organisation: https://github.com/NewLifeX

<details>
<summary>WeChat-Offizieller Account: 智能大石头 (zum Folgen scannen)</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>
