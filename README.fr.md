# NewLife.Melsec - 三菱PLC / Mitsubishi PLC

> **🌐 Langue**
>
> [🇨🇳 简体中文](README.zh-CN.md) · [🇺🇸 English](README.en.md) · [🇯🇵 日本語](README.ja.md) · [🇰🇷 한국어](README.ko.md) · [🇩🇪 Deutsch](README.de.md) · [🇫🇷 **Français**](README.fr.md) · [🇪🇸 Español](README.es.md) · [🇧🇷 Português](README.pt-BR.md) · [🇷🇺 Русский](README.ru.md) · [🇮🇹 Italiano](README.it.md)

---

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/NewLife.Melsec?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/NewLife.Melsec?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Melsec?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Melsec?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/NewLife.Melsec?label=dev%20nuget&logo=nuget)

Bibliothèque de communication .NET pour API Mitsubishi  

Source : https://github.com/NewLifeX/NewLife.Melsec  
NuGet : `NewLife.Melsec`  

## Présentation

**NewLife.Melsec** est une bibliothèque de communication purement gérée pour les API Mitsubishi, basée sur les spécifications d'interface standard **NewLife.IoT**. Elle couvre trois grandes familles de protocoles — le protocole série FxLinks, le protocole Ethernet MC (frames 3E/1E/4E) et le protocole SLMP — avec prise en charge du multi-transport TCP/UDP/Série. La bibliothèque est sous licence MIT et ne dépend d'aucun composant commercial.

Grâce à l'interface unifiée `IDriver`, elle peut être intégrée en une seule ligne de code aux plateformes passerelles **ZeroIoT/IoTEdge**.

## Principales caractéristiques

- **Couverture protocolaire complète** : Implémentation complète de FxLinks série (BR/WR/BW/WW), des frames MC Ethernet 3E/1E/4E (binaire+ASCII) et des frames SLMP 3C
- **Implémentation purement gérée** : Aucune dépendance à HslCommunication ou autres bibliothèques commerciales, uniquement les bibliothèques standard .NET
- **Intégration dans l'écosystème IoT** : Conforme aux spécifications NewLife.IoT v2.6+ `IDriver`/`IDriverParameter`/`INode`
- **Optimisation des lectures par lots** : L'algorithme `BuildSegments` fusionne automatiquement les adresses adjacentes
- **Support multi-station** : Plusieurs API peuvent partager une seule connexion série
- **Conversion de types de données** : `ConvertToWords` prend en charge Boolean/Int16/Int32/Float/Double → UInt16[]
- **Fonctionnalités avancées** : Frames 4E, lecture aléatoire, RUN/STOP à distance, verrouillage par mot de passe, enregistrement de surveillance
- **Tracing APM** : Tracing distribué via `ITracer`
- **Multi-plateforme** : 9 frameworks cibles de `net45` à `net10.0`

## Prise en charge des protocoles

| Protocole | Transport | Séries d'API supportées | Statut |
|:---------:|:---------:|:------------------------|:------:|
| FxLinks | RS-485 Série | Série FX | ✅ Terminé |
| MC 3E (Binaire/ASCII) | TCP/IP | Q, iQ-R, L, FX5U | ✅ Terminé |
| MC 1E | TCP/IP | Série A | ✅ Terminé |
| MC 4E | TCP/IP | Série iQ-R | ✅ Terminé |
| MC (UDP/Série) | UDP / Série | Multi-transport | ✅ Terminé |
| SLMP 3C | TCP/IP | iQ-R, iQ-F | ✅ Terminé |

## Démarrage rapide

```
dotnet add package NewLife.Melsec
```

Pour des exemples de code détaillés, veuillez consulter la version [anglaise](README.en.md) ou [chinoise](README.zh-CN.md).

## Documentation

| Document | Description |
|----------|-------------|
| [Définition des besoins](Doc/需求文档.md) | Vision, objectifs, exigences fonctionnelles |
| [Liste des fonctionnalités](Doc/功能清单.md) | Suivi 3D (implémentation/test/commentaire) |
| [Architecture](Doc/架构设计.md) | Architecture en couches, composants |
| [Analyse concurrentielle](Doc/竞品分析.md) | Matrice de comparaison et analyse des écarts |

## Questions fréquentes

**Q : Pourquoi la lecture FxLinks renvoie-t-elle null ?**  
R : La cause est une incohérence des paramètres du port série (débit en bauds/parité) ou un dépassement du délai de communication.

**Q : Quels modes de transport le protocole MC prend-il en charge ?**  
R : Il prend en charge trois modes : TCP (par défaut), UDP et Série.

## Équipe NewLife

L'équipe NewLife a été fondée en 2002 et se consacre à l'innovation des solutions IoT. Elle a publié plus de 80 projets open source avec plus de 4 millions de téléchargements NuGet.

Site web : https://newlifex.com  
GitHub : https://github.com/newlifex
