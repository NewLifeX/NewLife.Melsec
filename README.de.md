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

## NewLife-Team

Das NewLife-Team wurde 2002 gegründet und widmet sich der Innovation von IoT-Lösungen. Es hat über 80 Open-Source-Projekte veröffentlicht mit über 4 Millionen NuGet-Downloads.

Webseite: https://newlifex.com  
GitHub: https://github.com/newlifex
