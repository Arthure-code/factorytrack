# FactoryTrack — Indoor Asset Tracker (.NET)

Real-time indoor location system (RTLS) for tracking equipment inside a factory,
built with .NET 9, ASP.NET Core, gRPC, SignalR, PostgreSQL, TimescaleDB and
.NET MAUI.

> **Personal project.** The UWB/BLE hardware layer is simulated. See
> [Limitations](#limitations-and-next-steps).

*Résumé en français : système de localisation temps réel d'équipements en usine.
Les balises radio sont écoutées par des passerelles fixes ; le serveur calcule
les positions par trilatération, les historise et les diffuse aux clients. Un
client MAUI affiche le plan, la trace historique et les alertes de zones
interdites en direct.*

---

## Architecture

```
Balises BLE/UWB ──RSSI──> Passerelles ──gRPC──> Ingestion
                                                    │
                                          idempotence, hors ordre,
                                          fenêtre de regroupement
                                                    │
                                            Positionnement
                                       (RSSI→distance, trilatération, filtrage)
                                                    │
                                    ┌───────────────┴───────────────┐
                                    ▼                               ▼
                          PostgreSQL/TimescaleDB              SignalR Hub
                            (hypertable, agrégats)                  │
                                    │                               ▼
                                    └────REST────────────>    MAUI · Blazor
```

| Service | Rôle | Port |
|---|---|---|
| `FactoryTrack.Ingestion` | Endpoint gRPC, déduplication, calcul de position | 8081 |
| `FactoryTrack.Api` | REST + hub SignalR, surveillance du silence et des zones | 8080 |
| `FactoryTrack.Simulator` | Générateur de mesures radio | — |
| `FactoryTrack.Mobile` | Client .NET MAUI (Android + Windows) | — |
| `timescaledb` | Référentiel + série temporelle | 5432 |

### Projets

- **Domain** — entités, trilatération, filtrage. Aucune dépendance externe.
- **Contracts** — `.proto` et DTOs partagés entre serveur et clients.
- **Infrastructure** — EF Core, dépôts, cache du référentiel.
- **Mobile** — MAUI, MVVM (CommunityToolkit), SkiaSharp pour le plan.

---

## Démarrage

**Tout-en-un avec Docker :**

```bash
git clone <url>
cd factorytrack
docker compose up --build
```

**Sans Docker (dev local),** avec une instance PostgreSQL/TimescaleDB accessible
sur `localhost:5432` :

```bash
dotnet run --project src/FactoryTrack.Api          # port 8080
dotnet run --project src/FactoryTrack.Ingestion    # port 8081
dotnet run --project src/FactoryTrack.Simulator    # émet vers l'ingestion
```

Les fichiers `appsettings.Development.json` de chaque service pointent vers
`localhost` en dev.

**Client MAUI :**

```bash
dotnet build src/FactoryTrack.Mobile -f net9.0-windows10.0.19041.0
./src/FactoryTrack.Mobile/bin/Debug/net9.0-windows10.0.19041.0/win10-x64/FactoryTrack.Mobile.exe
```

Cibles : Android (émulateur → hôte via `10.0.2.2`) et Windows.

Sanity check :

```bash
curl http://localhost:8080/health
curl http://localhost:8080/api/equipements
curl http://localhost:8080/api/positions/etage/0
```

Documentation OpenAPI : `http://localhost:8080/openapi/v1.json`

### Tests

```bash
dotnet test
```

---

## Client MAUI

Ce que le client fait aujourd'hui :

- **Plan de l'usine** rendu avec SkiaSharp — passerelles, zones, équipements
  qui bougent en temps réel via SignalR.
- **Rayon d'incertitude** affichable en pointillé (toggle « Précision »).
- **Tap sur un équipement** → page détail avec trace des 30 dernières minutes
  (polyline dégradée), distance parcourue, précision moyenne.
- **Bannière rouge** à l'entrée d'un équipement dans une zone interdite ; le
  point vire au rouge sur le plan tant qu'il y reste. Uniquement à la
  transition — pas de spam si l'équipement stagne.
- **Reconnexion SignalR automatique** avec resynchronisation REST : les
  messages perdus pendant la coupure ne reviennent pas seuls.
- **Un `HubConnection` unique** injecté en singleton, jamais un par page.

Conventions respectées : MVVM strict via `CommunityToolkit.Mvvm`
(`[ObservableProperty]`, `[RelayCommand]`), navigation par `Shell` avec routes
nommées, ViewModel injecté au constructeur, styles centralisés, cleartext HTTP
autorisé côté Android uniquement pour la V1 locale.

---

## Points techniques

**gRPC en entrée, SignalR en sortie.** gRPC pour un flux montant dense et
binaire venant d'appareils contraints ; SignalR pour pousser vers des clients
hétérogènes avec reconnexion automatique. Chacun a un rôle distinct.

**Idempotence.** Une mesure est identifiée par `(balise, passerelle, horodatage)`.
Le simulateur injecte volontairement 3 % de doublons pour l'éprouver.

**Données hors ordre.** Une mesure antérieure à la dernière traitée est rejetée
plutôt qu'appliquée : sinon l'équipement reculerait sur le plan.

**Fenêtre de regroupement.** Les mesures d'une même balise sont accumulées
jusqu'à ce qu'on ait toutes les passerelles actives, ou jusqu'à expiration de
`FenetreRegroupementMs`. Ni trop tôt (ancres gaspillées), ni indéfiniment (une
passerelle muette bloquerait le calcul).

**Indicateur « last seen ».** Au-delà de 30 secondes sans mesure, l'équipement
est signalé comme silencieux. Notification à la transition uniquement.

**Alerte zone interdite.** Un service de fond scanne les dernières positions
toutes les 5 s et émet `AlerteZoneEntree` / `AlerteZoneSortie` uniquement aux
transitions. Le client MAUI transforme ça en bannière et coloration du point.

**Indice de précision.** UWB et Bluetooth n'ont pas la même fiabilité physique.
La précision estimée remonte jusqu'à l'affichage sous forme de rayon
d'incertitude.

**Filtrage.** Lissage exponentiel appliqué à la position calculée, jamais au
RSSI brut, avec amortissement renforcé des sauts aberrants.

---

## Décisions d'architecture

Les ADR documentent aussi les décisions **négatives** — ce qui n'a pas été
fait, et pourquoi :

| ADR | Sujet |
|---|---|
| [0001](docs/adr/0001-repere-local-en-metres.md) | Repère local en mètres plutôt que lat/long |
| [0002](docs/adr/0002-pas-de-courtier-de-messages-en-v1.md) | Pas de RabbitMQ en V1 |
| [0003](docs/adr/0003-schema-sql-plutot-que-migrations.md) | Schéma SQL plutôt que migrations EF |
| [0004](docs/adr/0004-calcul-de-position-cote-serveur.md) | Calcul côté serveur |

---

## Limitations and next steps

**Ce qui est simulé.** Aucune balise UWB ni passerelle physique n'est utilisée.
Le simulateur produit les RSSI qu'auraient mesurés des passerelles réelles, à
partir d'une position connue — ce qui permet en retour de comparer la position
calculée à la vérité terrain, et d'injecter pertes, doublons et bruit gaussien
à des taux configurables. C'est un outil de test de charge, pas un substitut
au matériel.

**Ce qui manque encore.**

- Authentification JWT/OIDC (le hub `DiffuserPosition` est public en V1)
- Tests d'intégration avec Testcontainers
- Migrations EF Core (voir ADR 0003)
- Multi-étages dans le client MAUI (le back-end le supporte)
- Client Blazor WebAssembly pour la consultation à distance
- Panel latéral avec liste triable/filtrable des équipements

**Mesures de performance.** Non encore réalisées. La cible visée est 500
balises à 2 positions/seconde, avec latence et débit mesurés plutôt
qu'estimés.

---

## Stack

.NET 9 · ASP.NET Core · gRPC · SignalR · Entity Framework Core ·
PostgreSQL · TimescaleDB · .NET MAUI · SkiaSharp · CommunityToolkit.Mvvm ·
Docker Compose · xUnit · FluentAssertions · Serilog · GitHub Actions ·
GitLab CI
