# NewLife.Melsec - 三菱PLC

> **🌐 言語**
>
> [🇨🇳 简体中文](README.zh-CN.md) · [🇺🇸 English](README.en.md) · [🇯🇵 **日本語**](README.ja.md) · [🇰🇷 한국어](README.ko.md) · [🇩🇪 Deutsch](README.de.md) · [🇫🇷 Français](README.fr.md) · [🇪🇸 Español](README.es.md) · [🇧🇷 Português](README.pt-BR.md) · [🇷🇺 Русский](README.ru.md) · [🇮🇹 Italiano](README.it.md)

---

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/NewLife.Melsec?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/NewLife.Melsec?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Melsec?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Melsec?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/NewLife.Melsec?label=dev%20nuget&logo=nuget)

三菱PLC通信用.NETライブラリ  

ソース： https://github.com/NewLifeX/NewLife.Melsec  
NuGet： `NewLife.Melsec`  

## 概要

**NewLife.Melsec** は **NewLife.IoT** 標準インターフェース仕様に基づいた三菱PLC用の純管理型通信ライブラリです。FxLinks シリアルプロトコル、MC イーサネットプロトコル（3E/1E/4E フレーム）、SLMP プロトコルの3つの主要プロトコルファミリをカバーし、TCP/UDP/シリアルのマルチトランスポートに対応しています。MITライセンスで提供され、商用コンポーネントへの依存はありません。

統一された `IDriver` インターフェースを通じて、**ZeroIoT/IoTEdge** ゲートウェイプラットフォームにワンコードで統合できます。

## 主な特徴

- **完全なプロトコルカバレッジ**：FxLinks シリアル（BR/WR/BW/WW）、MC イーサネット 3E/1E/4E フレーム（バイナリ+ASCII）、SLMP 3C フレームを完全実装
- **純管理型実装**：HslCommunication などの商用ライブラリに依存せず、.NET 標準ライブラリのみを使用
- **IoTエコシステム統合**：NewLife.IoT v2.6+ の `IDriver`/`IDriverParameter`/`INode` 仕様に準拠
- **バッチ読み取り最適化**：`BuildSegments` アルゴリズムが隣接アドレスを自動マージ
- **マルチステーション対応**：複数のPLCが1つのシリアルポート接続を共有可能
- **データ型変換**：`ConvertToWords` が Boolean/Int16/Int32/Float/Double → UInt16[] をサポート
- **高度なプロトコル機能**：4E フレーム、ランダム読み取り、遠隔 RUN/STOP、パスワードロック、モニタ登録
- **APM トレーシング**：`ITracer` による分散トレーシング
- **クロスプラットフォーム**：`net45` から `net10.0` まで9つのターゲットフレームワーク

## プロトコルサポート

| プロトコル | トランスポート | 対応PLCシリーズ | 状態 |
|:--------:|:------------:|:---------------|:---:|
| FxLinks | RS-485 シリアル | FX シリーズ | ✅ 完了 |
| MC 3E (Binary/ASCII) | TCP/IP | Q, iQ-R, L, FX5U | ✅ 完了 |
| MC 1E | TCP/IP | A シリーズ | ✅ 完了 |
| MC 4E | TCP/IP | iQ-R シリーズ | ✅ 完了 |
| MC (UDP/Serial) | UDP / シリアル | マルチトランスポート | ✅ 完了 |
| SLMP 3C | TCP/IP | iQ-R, iQ-F | ✅ 完了 |

## クイックスタート

```
dotnet add package NewLife.Melsec
```

詳細なコード例は [English版](README.en.md) または [中文版](README.zh-CN.md) を参照してください。

## ドキュメント

| ドキュメント | 説明 |
|-------------|------|
| [要件定義](Doc/需求文档.md) | ビジョン、目標、機能要件 |
| [機能一覧](Doc/功能清单.md) | 実装/テスト/コメントの3Dトラッキング |
| [アーキテクチャ](Doc/架构设计.md) | 階層アーキテクチャ、コンポーネント |
| [競合分析](Doc/竞品分析.md) | 比較マトリックスとギャップ分析 |

## よくある質問

**Q: FxLinks 読み取りが null を返す原因は？**  
A: シリアルポートパラメータの不一致（ボーレート/パリティ）または通信タイムアウトが原因です。

**Q: MC プロトコルはどのトランスポートをサポートしていますか？**  
A: TCP（デフォルト）、UDP、シリアルの3つのモードをサポートしています。

## NewLife チーム

NewLife チームは2002年に設立され、IoTソリューションの革新に取り組んでいます。80以上のオープンソースプロジェクトを公開し、NuGetダウンロード数は400万以上です。

Webサイト：https://newlifex.com  
GitHub：https://github.com/newlifex
