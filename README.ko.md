# NewLife.Melsec - 三菱PLC / Mitsubishi PLC

> **🌐 언어**
>
> [🇨🇳 简体中文](README.zh-CN.md) · [🇺🇸 English](README.en.md) · [🇯🇵 日本語](README.ja.md) · [🇰🇷 **한국어**](README.ko.md) · [🇩🇪 Deutsch](README.de.md) · [🇫🇷 Français](README.fr.md) · [🇪🇸 Español](README.es.md) · [🇧🇷 Português](README.pt-BR.md) · [🇷🇺 Русский](README.ru.md) · [🇮🇹 Italiano](README.it.md)

---

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/NewLife.Melsec?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/NewLife.Melsec?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Melsec?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Melsec?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/NewLife.Melsec?label=dev%20nuget&logo=nuget)

Mitsubishi PLC 통신을 위한 .NET 라이브러리  

소스: https://github.com/NewLifeX/NewLife.Melsec  
NuGet: `NewLife.Melsec`  

## 개요

**NewLife.Melsec**는 **NewLife.IoT** 표준 인터페이스 사양을 기반으로 한 Mitsubishi PLC용 순수 관리형 통신 라이브러리입니다. FxLinks 시리얼 프로토콜, MC 이더넷 프로토콜(3E/1E/4E 프레임), SLMP 프로토콜의 세 가지 주요 프로토콜 제품군을 포괄하며 TCP/UDP/시리얼 멀티 전송을 지원합니다. MIT 라이선스로 제공되며 상용 구성 요소에 대한 의존성이 없습니다.

통합된 `IDriver` 인터페이스를 통해 **ZeroIoT/IoTEdge** 게이트웨이 플랫폼에 원코드로 통합할 수 있습니다.

## 주요 기능

- **완전한 프로토콜 지원**: FxLinks 시리얼(BR/WR/BW/WW), MC 이더넷 3E/1E/4E 프레임(바이너리+ASCII), SLMP 3C 프레임 완전 구현
- **순수 관리형 구현**: HslCommunication 등의 상용 라이브러리에 의존하지 않으며 .NET 표준 라이브러리만 사용
- **IoT 에코시스템 통합**: NewLife.IoT v2.6+의 `IDriver`/`IDriverParameter`/`INode` 사양 준수
- **배치 읽기 최적화**: `BuildSegments` 알고리즘이 인접 주소를 자동 병합
- **멀티 스테이션 지원**: 여러 PLC가 하나의 시리얼 포트 연결을 공유 가능
- **데이터 형식 변환**: `ConvertToWords`가 Boolean/Int16/Int32/Float/Double → UInt16[] 변환 지원
- **고급 프로토콜 기능**: 4E 프레임, 랜덤 읽기, 원격 RUN/STOP, 비밀번호 잠금, 모니터 등록
- **APM 추적**: `ITracer`를 통한 분산 추적 지원
- **크로스 플랫폼**: `net45`부터 `net10.0`까지 9개의 대상 프레임워크 지원

## 프로토콜 지원

| 프로토콜 | 전송 방식 | 지원 PLC 시리즈 | 상태 |
|:--------:|:--------:|:---------------|:----:|
| FxLinks | RS-485 시리얼 | FX 시리즈 | ✅ 완료 |
| MC 3E (Binary/ASCII) | TCP/IP | Q, iQ-R, L, FX5U | ✅ 완료 |
| MC 1E | TCP/IP | A 시리즈 | ✅ 완료 |
| MC 4E | TCP/IP | iQ-R 시리즈 | ✅ 완료 |
| MC (UDP/Serial) | UDP / 시리얼 | 멀티 전송 | ✅ 완료 |
| SLMP 3C | TCP/IP | iQ-R, iQ-F | ✅ 완료 |

## 퀵스타트

```
dotnet add package NewLife.Melsec
```

자세한 코드 예제는 [English版](README.en.md) 또는 [中文版](README.zh-CN.md)을 참조하세요.

## 문서

| 문서 | 설명 |
|------|------|
| [요구사항 정의](Doc/需求文档.md) | 비전, 목표, 기능 요구사항 |
| [기능 목록](Doc/功能清单.md) | 구현/테스트/주석 3D 추적 |
| [아키텍처](Doc/架构设计.md) | 계층 아키텍처, 구성 요소 |
| [경쟁사 분석](Doc/竞品分析.md) | 비교 매트릭스 및 격차 분석 |

## 자주 묻는 질문

**Q: FxLinks 읽기가 null을 반환하는 이유는?**  
A: 시리얼 포트 매개변수 불일치(전송 속도/패리티) 또는 통신 타임아웃이 원인입니다.

**Q: MC 프로토콜은 어떤 전송 방식을 지원하나요?**  
A: TCP(기본값), UDP, 시리얼의 세 가지 모드를 지원합니다.

## NewLife 팀

NewLife 팀은 2002년에 설립되어 IoT 솔루션 혁신에 전념하고 있습니다. 80개 이상의 오픈소스 프로젝트를 출시했으며 NuGet 다운로드 수는 400만 이상입니다.

웹사이트: https://newlifex.com  
GitHub: https://github.com/newlifex
