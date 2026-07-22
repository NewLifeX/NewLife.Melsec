# NewLife.Melsec - 三菱PLC / Mitsubishi PLC

> **🌐 Idioma**
>
> [🇨🇳 简体中文](README.zh-CN.md) · [🇺🇸 English](README.en.md) · [🇯🇵 日本語](README.ja.md) · [🇰🇷 한국어](README.ko.md) · [🇩🇪 Deutsch](README.de.md) · [🇫🇷 Français](README.fr.md) · [🇪🇸 **Español**](README.es.md) · [🇧🇷 Português](README.pt-BR.md) · [🇷🇺 Русский](README.ru.md) · [🇮🇹 Italiano](README.it.md)

---

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/NewLife.Melsec?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/NewLife.Melsec?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Melsec?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Melsec?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/NewLife.Melsec?label=dev%20nuget&logo=nuget)

Biblioteca de comunicación .NET para PLC Mitsubishi  

Fuente: https://github.com/NewLifeX/NewLife.Melsec  
NuGet: `NewLife.Melsec`  

## Descripción general

**NewLife.Melsec** es una biblioteca de comunicación totalmente administrada para PLC Mitsubishi, construida sobre la especificación de interfaz estándar **NewLife.IoT**. Cubre tres familias principales de protocolos — el protocolo serie FxLinks, el protocolo Ethernet MC (tramas 3E/1E/4E) y el protocolo SLMP — con soporte de transporte múltiple TCP/UDP/Serie. La biblioteca tiene licencia MIT y no depende de ningún componente comercial.

A través de la interfaz unificada `IDriver`, se puede integrar con una sola línea de código en las plataformas de puerta de enlace **ZeroIoT/IoTEdge**.

## Características principales

- **Cobertura completa de protocolos**: Implementación completa de FxLinks serie (BR/WR/BW/WW), tramas MC Ethernet 3E/1E/4E (binario+ASCII), tramas SLMP 3C
- **Implementación totalmente administrada**: Sin dependencia de HslCommunication u otras bibliotecas comerciales, solo bibliotecas estándar .NET
- **Integración con ecosistema IoT**: Conforme a las especificaciones NewLife.IoT v2.6+ `IDriver`/`IDriverParameter`/`INode`
- **Optimización de lectura por lotes**: El algoritmo `BuildSegments` fusiona automáticamente direcciones adyacentes
- **Soporte multiestación**: Múltiples PLC pueden compartir una sola conexión de puerto serie
- **Conversión de tipos de datos**: `ConvertToWords` admite Boolean/Int16/Int32/Float/Double → UInt16[]
- **Funciones avanzadas**: Tramas 4E, lectura aleatoria, RUN/STOP remoto, bloqueo por contraseña, registro de monitoreo
- **Trazabilidad APM**: Trazabilidad distribuida mediante `ITracer`
- **Multiplataforma**: 9 frameworks objetivo desde `net45` hasta `net10.0`

## Soporte de protocolos

| Protocolo | Transporte | Series de PLC compatibles | Estado |
|:---------:|:----------:|:--------------------------|:------:|
| FxLinks | RS-485 Serie | Serie FX | ✅ Completado |
| MC 3E (Binario/ASCII) | TCP/IP | Q, iQ-R, L, FX5U | ✅ Completado |
| MC 1E | TCP/IP | Serie A | ✅ Completado |
| MC 4E | TCP/IP | Serie iQ-R | ✅ Completado |
| MC (UDP/Serie) | UDP / Serie | Multi-transporte | ✅ Completado |
| SLMP 3C | TCP/IP | iQ-R, iQ-F | ✅ Completado |

## Inicio rápido

```
dotnet add package NewLife.Melsec
```

Para ver ejemplos de código detallados, consulte la versión en [inglés](README.en.md) o [chino](README.zh-CN.md).

## Documentación

| Documento | Descripción |
|-----------|-------------|
| [Definición de requisitos](Doc/需求文档.md) | Visión, objetivos, requisitos funcionales |
| [Lista de funciones](Doc/功能清单.md) | Seguimiento 3D (implementación/prueba/comentario) |
| [Arquitectura](Doc/架构设计.md) | Arquitectura en capas, componentes |
| [Análisis competitivo](Doc/竞品分析.md) | Matriz de comparación y análisis de brechas |

## Preguntas frecuentes

**P: ¿Por qué la lectura de FxLinks devuelve null?**  
R: La causa es una discrepancia en los parámetros del puerto serie (velocidad de transmisión/paridad) o un tiempo de espera de comunicación agotado.

**P: ¿Qué modos de transporte admite el protocolo MC?**  
R: Admite tres modos: TCP (predeterminado), UDP y Serie.

## Equipo NewLife

El equipo NewLife fue fundado en 2002 y se dedica a la innovación de soluciones IoT. Ha publicado más de 80 proyectos de código abierto con más de 4 millones de descargas en NuGet.

Sitio web: https://newlifex.com  
GitHub: https://github.com/newlifex
