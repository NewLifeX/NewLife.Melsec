# NewLife.Melsec - 三菱PLC / Mitsubishi PLC

> **🌐 Idioma**
>
> [🇨🇳 简体中文](README.zh-CN.md) · [🇺🇸 English](README.en.md) · [🇯🇵 日本語](README.ja.md) · [🇰🇷 한국어](README.ko.md) · [🇩🇪 Deutsch](README.de.md) · [🇫🇷 Français](README.fr.md) · [🇪🇸 Español](README.es.md) · [🇧🇷 **Português**](README.pt-BR.md) · [🇷🇺 Русский](README.ru.md) · [🇮🇹 Italiano](README.it.md)

---

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/NewLife.Melsec?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/NewLife.Melsec?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Melsec?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Melsec?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/NewLife.Melsec?label=dev%20nuget&logo=nuget)

Biblioteca de comunicação .NET para CLP Mitsubishi  

Fonte: https://github.com/NewLifeX/NewLife.Melsec  
NuGet: `NewLife.Melsec`  

## Visão geral

**NewLife.Melsec** é uma biblioteca de comunicação puramente gerenciada para CLPs Mitsubishi, construída sobre a especificação de interface padrão **NewLife.IoT**. Ela cobre três grandes famílias de protocolos — o protocolo serial FxLinks, o protocolo Ethernet MC (frames 3E/1E/4E) e o protocolo SLMP — com suporte a transporte múltiplo TCP/UDP/Serial. A biblioteca é licenciada sob MIT e não depende de nenhum componente comercial.

Através da interface unificada `IDriver`, pode ser integrada com uma única linha de código às plataformas de gateway **ZeroIoT/IoTEdge**.

## Principais recursos

- **Cobertura completa de protocolos**: Implementação completa de FxLinks serial (BR/WR/BW/WW), frames MC Ethernet 3E/1E/4E (binário+ASCII), frames SLMP 3C
- **Implementação puramente gerenciada**: Sem dependência de HslCommunication ou outras bibliotecas comerciais, apenas bibliotecas padrão .NET
- **Integração com ecossistema IoT**: Conforme às especificações NewLife.IoT v2.6+ `IDriver`/`IDriverParameter`/`INode`
- **Otimização de leitura em lote**: O algoritmo `BuildSegments` mescla automaticamente endereços adjacentes
- **Suporte a múltiplas estações**: Vários CLPs podem compartilhar uma única conexão serial
- **Conversão de tipos de dados**: `ConvertToWords` suporta Boolean/Int16/Int32/Float/Double → UInt16[]
- **Recursos avançados**: Frames 4E, leitura aleatória, RUN/STOP remoto, bloqueio por senha, registro de monitoramento
- **Rastreamento APM**: Rastreamento distribuído via `ITracer`
- **Multiplataforma**: 9 frameworks alvo do `net45` ao `net10.0`

## Suporte a protocolos

| Protocolo | Transporte | Séries de CLP compatíveis | Status |
|:---------:|:----------:|:--------------------------|:------:|
| FxLinks | RS-485 Serial | Série FX | ✅ Concluído |
| MC 3E (Binário/ASCII) | TCP/IP | Q, iQ-R, L, FX5U | ✅ Concluído |
| MC 1E | TCP/IP | Série A | ✅ Concluído |
| MC 4E | TCP/IP | Série iQ-R | ✅ Concluído |
| MC (UDP/Serial) | UDP / Serial | Multi-transporte | ✅ Concluído |
| SLMP 3C | TCP/IP | iQ-R, iQ-F | ✅ Concluído |

## Início rápido

```
dotnet add package NewLife.Melsec
```

Para exemplos de código detalhados, consulte a versão em [inglês](README.en.md) ou [chinês](README.zh-CN.md).

## Documentação

| Documento | Descrição |
|-----------|-------------|
| [Definição de requisitos](Doc/需求文档.md) | Visão, objetivos, requisitos funcionais |
| [Lista de funcionalidades](Doc/功能清单.md) | Rastreamento 3D (implementação/teste/comentário) |
| [Arquitetura](Doc/架构设计.md) | Arquitetura em camadas, componentes |
| [Análise competitiva](Doc/竞品分析.md) | Matriz de comparação e análise de lacunas |

## Perguntas frequentes

**P: Por que a leitura do FxLinks retorna null?**  
R: A causa é uma incompatibilidade nos parâmetros da porta serial (taxa de transmissão/paridade) ou tempo limite de comunicação excedido.

**P: Quais modos de transporte o protocolo MC suporta?**  
R: Ele suporta três modos: TCP (padrão), UDP e Serial.

## Equipe NewLife

A equipe NewLife foi fundada em 2002 e é dedicada à inovação de soluções IoT. Já publicou mais de 80 projetos de código aberto com mais de 4 milhões de downloads no NuGet.

Site: https://newlifex.com  
GitHub: https://github.com/newlifex
