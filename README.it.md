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

## Mappa dell'ecosistema

NewLife è un ecosistema a livelli basato sulle dipendenze: ogni progetto ha versione e rilascio indipendenti. La mappa seguente è identica alla lettera in tutti i repository NewLife; ogni progetto rimanda al proprio repository di codice e al pacchetto NuGet principale.

| Progetto | Posizionamento in una riga | Download |
| --- | --- | --- |
| **Fondamenta** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | Log, configurazione, cache, rete, serializzazione, pianificazione e tracciamento APM. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | Middleware dati: singola tabella su scala di decine di miliardi di righe, partizionamento automatico, separazione lettura/scrittura, oltre 10 database supportati. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | Libreria di rete ad alte prestazioni: 1,77 milioni di pps e 14,5 Gbps su singola macchina (TCP 1KB, misurati 2026-10), 4 milioni di connessioni TCP persistenti. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | Framework di comunicazione a protocollo e RPC per applicazioni client/server e dispositivi IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | Installa le applicazioni .NET come daemon del sistema operativo: servizi Windows e systemd Linux. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | Modelli di progetto `dotnet new`: console, Web, WebApi, servizi, dati e IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **Dati e messaggistica** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Client Redis: latenza in microsecondi, throughput nell'ordine dei milioni di operazioni, coda di messaggi integrata. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Client RocketMQ completamente gestito, con supporto ad Apache RocketMQ e Alibaba Cloud Message Queue. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | Client e server MQTT; il client supporta Alibaba Cloud IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | Database ibrido in C#: modalità embedded e di rete, relazionale, serie temporali, coda di messaggi e KV in un unico prodotto. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | Driver MySql e livello ausiliario di accesso ai dati, basati sullo stack dati NewLife. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web, AI e distribuito** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | Piattaforma di sviluppo rapido Cube: permessi utente, SSO, server OAuth e scaffolding CRUD. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | Piattaforma di servizi distribuiti Stardust: gestione nodi, APM, centro di configurazione, registro e centro di rilascio. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | AntJob: pianificazione distribuita di attività e dati in tempo reale, con partizionamento, retry e ricalcolo retroattivo. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | Accesso unificato a 46 fornitori di modelli di linguaggio, con l'applicazione web di chat ChatAI pronta all'uso. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **Industria e IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | Libreria standard IoT che definisce i contratti dei protocolli di comunicazione e le astrazioni dei dispositivi. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Modbus TCP/RTU/ASCII, implementato su NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | Protocollo PLC Siemens, implementato su NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | Protocollo PLC Omron, implementato su NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | Protocollo PLC Mitsubishi Melsec, implementato su NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | Client OPC per l'acquisizione di dati industriali. | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | Stack del protocollo di building automation BACnet. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | Piattaforma IoT leggera open source, con gateway e gestione dispositivi. | — |
| **Telematica veicolare** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | Protocollo di localizzazione veicolare JT/T 808: oltre 70 corpi messaggio, versioni 2011/2013/2019. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | Protocollo di interconnessione tra piattaforme JT/T 809 per le autorità dei trasporti. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | Video veicolare JT/T 1078: parsing RTP, remux FLV/fMP4 e push in diretta. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **Componenti di dominio** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | Un unico pacchetto per Excel, Word, PPT, PDF e oltre 15 formati documento. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | Audio multipiattaforma: codec, I/O dispositivi, DSP, container e streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | Codec video, formati container, elaborazione dei frame e streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | Interfaccia unificata per Baidu/AMap/Tencent/Tianditu, con geocoding e conversione di coordinate. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | Geolocalizzazione IPv4: libreria offline mappata in memoria più API online come doppia fonte. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **Strumenti e prodotti** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | Toolbox per sviluppatori: rete, seriale, crittografia, regex, Modbus e MQTT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | Sistema self-hosted di gestione del codice sorgente Git. | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | Installa automaticamente il runtime .NET corrispondente quando viene avviato da un host meno recente. | — |

> I badge mostrano il pacchetto principale di ciascun progetto; pacchetti tema, di estensione e satelliti sono elencati nelle rispettive pagine NuGet.Gli oltre 30 pacchetti non elencati qui (FTP, LoRa, estensioni Office, driver di protocollo e altri) sono disponibili nell'[organizzazione NewLifeX](https://github.com/NewLifeX) e su [newlifex.com](https://newlifex.com).

## Team di sviluppo NewLife

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

Il team NewLife, fondato nel 2002, da oltre vent'anni si dedica all'ecosistema .NET e IoT, guida quasi un centinaio di progetti open source (34 progetti principali nella mappa dell'ecosistema) e offre soluzioni integrate hardware e software per energia, logistica, sanità, automazione industriale e internet; il nostro motto è “nell'apprendimento non ci sono anziani e giovani, chi padroneggia merita rispetto”.

I componenti open source hanno totalizzato <!--nuget-total:start-->6.48M<!--nuget-total:end--> download su NuGet (intero ecosistema, ottobre 2026).

Sito web: https://newlifex.com · Organizzazione open source: https://github.com/NewLifeX

<details>
<summary>Account WeChat ufficiale: 智能大石头 (scansiona per seguire)</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>
