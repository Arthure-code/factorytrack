# FactoryTrack — Indoor Asset Tracker (.NET)

Real-time indoor location system (RTLS) for tracking equipment inside a factory,
built with .NET 10, ASP.NET Core, gRPC, SignalR, PostgreSQL and TimescaleDB.

> **Personal project.** The UWB/BLE hardware layer is simulated. See
> [Limitations](#limitations-and-next-steps).

*Résumé en français : système de localisation temps réel d'équipements en usine.
Les balises radio sont écoutées par des passerelles fixes ; le serveur calcule
les positions par trilatération, les historise et les diffuse aux clients.*

---

## Architecture

```
Balises BLE/UWB ──RSSI──> Passerelles ──gRPC──> Ingestion
                                                    │
                                          idempotence, hors ordre
                                                    │
                                            Positionnement
                                       (RSSI→distance, trilatération, filtrage)
                                                    │
                                    ┌───────────────┴───────────────┐
                                    ▼                               ▼
                          PostgreSQL/TimescaleDB              SignalR Hub
                            (hypertable, agrégats)                  │
                                    │                               ▼
                                    └────REST────────────> MAUI · Blazor · Web
```

| Service | Rôle | Port |
|---|---|---|
| `FactoryTrack.Ingestion` | Endpoint gRPC, déduplication, calcul de position | 8081 |
| `FactoryTrack.Api` | REST + hub SignalR, surveillance du silence | 8080 |
| `FactoryTrack.Simulator` | Générateur de mesures radio | — |
| `timescaledb` | Référentiel + série temporelle | 5432 |

### Projets

- **Domain** — entités, trilatération, filtrage. Aucune dépendance externe.
- **Contracts** — `.proto` et DTOs partagés entre serveur et clients.
- **Infrastructure** — EF Core, dépôts, cache du référentiel.

---

## Démarrage

```bash
git clone <url>
cd factorytrack
docker compose up --build
```

Au bout d'environ une minute :

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

## Points techniques

**gRPC en entrée, SignalR en sortie.** gRPC pour un flux montant dense et binaire
venant d'appareils contraints ; SignalR pour pousser vers des clients hétérogènes
avec reconnexion automatique. Chacun a un rôle distinct.

**Idempotence.** Une mesure est identifiée par `(balise, passerelle, horodatage)`.
Le simulateur injecte volontairement 3 % de doublons pour l'éprouver.

**Données hors ordre.** Une mesure antérieure à la dernière traitée est rejetée
plutôt qu'appliquée : sinon l'équipement reculerait sur le plan.

**Indicateur « last seen ».** Au-delà de 30 secondes sans mesure, l'équipement est
signalé comme silencieux. Afficher une position périmée comme actuelle serait un
mensonge fonctionnel.

**Indice de précision.** UWB et Bluetooth n'ont pas la même fiabilité physique.
La précision estimée remonte jusqu'à l'affichage sous forme de rayon d'incertitude.

**Filtrage.** Lissage exponentiel appliqué à la position calculée, jamais au RSSI
brut, avec amortissement renforcé des sauts aberrants.

---

## Décisions d'architecture

Les ADR documentent aussi les décisions **négatives** — ce qui n'a pas été fait,
et pourquoi :

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
calculée à la vérité terrain, et d'injecter pertes, doublons et bruit gaussien à
des taux configurables. C'est un outil de test de charge, pas un substitut au
matériel.

**Ce qui manque.**

- Client mobile .NET MAUI (plan d'usine, marqueurs temps réel)
- Client Blazor WebAssembly pour la consultation à distance
- Authentification JWT/OIDC
- Tests d'intégration avec Testcontainers
- Migrations EF Core (voir ADR 0003)
- Alertes de sortie de zone
- Multi-étages

**Mesures de performance.** Non encore réalisées. La cible visée est 500 balises
à 2 positions/seconde, avec latence et débit mesurés plutôt qu'estimés.

---

## Stack

.NET 10 · ASP.NET Core · gRPC · SignalR · Entity Framework Core ·
PostgreSQL · TimescaleDB · Docker Compose · xUnit · FluentAssertions ·
Serilog · GitHub Actions · GitLab CI
