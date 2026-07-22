# NewLife.Melsec - 三菱PLC / Mitsubishi PLC

> **🌐 Lingua**
>
> [🇨🇳 简体中文](README.zh-CN.md) · [🇺🇸 English](README.en.md) · [🇯🇵 日本語](README.ja.md) · [🇰🇷 한국어](README.ko.md) · [🇩🇪 Deutsch](README.de.md) · [🇫🇷 Français](README.fr.md) · [🇪🇸 Español](README.es.md) · [🇧🇷 Português](README.pt-BR.md) · [🇷🇺 Русский](README.ru.md) · [🇮🇹 **Italiano**](README.it.md)

---

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/NewLife.Melsec?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/NewLife.Melsec?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Melsec?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Melsec?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/NewLife.Melsec?label=dev%20nuget&logo=nuget)

Libreria di comunicazione .NET per PLC Mitsubishi  

Sorgente: https://github.com/NewLifeX/NewLife.Melsec  
NuGet: `NewLife.Melsec`  

## Panoramica

**NewLife.Melsec** è una libreria di comunicazione puramente gestita per PLC Mitsubishi, basata sulle specifiche dell'interfaccia standard **NewLife.IoT**. Copre tre principali famiglie di protocolli — il protocollo seriale FxLinks, il protocollo Ethernet MC (frame 3E/1E/4E) e il protocollo SLMP — con supporto multi-trasporto TCP/UDP/Seriale. La libreria è concessa in licenza MIT e non ha dipendenze da componenti commerciali.

Attraverso l'interfaccia unificata `IDriver`, può essere integrata con una singola riga di codice nelle piattaforme gateway **ZeroIoT/IoTEdge**.

## Caratteristiche principali

- **Copertura completa dei protocolli**: Implementazione completa di FxLinks seriale (BR/WR/BW/WW), frame MC Ethernet 3E/1E/4E (binario+ASCII), frame SLMP 3C
- **Implementazione puramente gestita**: Nessuna dipendenza da HslCommunication o altre librerie commerciali, solo librerie standard .NET
- **Integrazione nell'ecosistema IoT**: Conforme alle specifiche NewLife.IoT v2.6+ `IDriver`/`IDriverParameter`/`INode`
- **Ottimizzazione lettura batch**: L'algoritmo `BuildSegments` unisce automaticamente gli indirizzi adiacenti
- **Supporto multi-stazione**: Più PLC possono condividere una singola connessione seriale
- **Conversione tipi di dati**: `ConvertToWords` supporta Boolean/Int16/Int32/Float/Double → UInt16[]
- **Funzionalità avanzate**: Frame 4E, lettura casuale, RUN/STOP remoto, blocco password, registrazione monitor
- **Tracciamento APM**: Tracciamento distribuito tramite `ITracer`
- **Multipiattaforma**: 9 framework di destinazione da `net45` a `net10.0`

## Supporto protocolli

| Protocollo | Trasporto | Serie PLC supportate | Stato |
|:----------:|:---------:|:---------------------|:-----:|
| FxLinks | RS-485 Seriale | Serie FX | ✅ Completato |
| MC 3E (Binario/ASCII) | TCP/IP | Q, iQ-R, L, FX5U | ✅ Completato |
| MC 1E | TCP/IP | Serie A | ✅ Completato |
| MC 4E | TCP/IP | Serie iQ-R | ✅ Completato |
| MC (UDP/Seriale) | UDP / Seriale | Multi-trasporto | ✅ Completato |
| SLMP 3C | TCP/IP | iQ-R, iQ-F | ✅ Completato |

## Avvio rapido

```
dotnet add package NewLife.Melsec
```

Per esempi di codice dettagliati, consultare la versione in [inglese](README.en.md) o [cinese](README.zh-CN.md).

## Documentazione

| Documento | Descrizione |
|-----------|-------------|
| [Definizione dei requisiti](Doc/需求文档.md) | Visione, obiettivi, requisiti funzionali |
| [Elenco funzionalità](Doc/功能清单.md) | Tracciamento 3D (implementazione/test/commento) |
| [Architettura](Doc/架构设计.md) | Architettura a livelli, componenti |
| [Analisi competitiva](Doc/竞品分析.md) | Matrice di confronto e analisi delle lacune |

## Domande frequenti

**D: Perché la lettura FxLinks restituisce null?**  
R: La causa è una discrepanza nei parametri della porta seriale (baud rate/parità) o un timeout di comunicazione.

**D: Quali modalità di trasporto supporta il protocollo MC?**  
R: Supporta tre modalità: TCP (predefinita), UDP e Seriale.

## Team NewLife

Il team NewLife è stato fondato nel 2002 e si dedica all'innovazione delle soluzioni IoT. Ha pubblicato oltre 80 progetti open source con più di 4 milioni di download su NuGet.

Sito web: https://newlifex.com  
GitHub: https://github.com/newlifex
