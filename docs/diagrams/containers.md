# Containers

Level 2 of the C4 model. Zoom into FactoryTrack : services deployables et leurs
technologies. Le detail interne des classes reste hors scope de cette vue.

```mermaid
flowchart LR
    passerelles["📶 Passerelles<br/>(ou Simulator)"]
    mobile["📱 FactoryTrack.Mobile<br/>.NET MAUI + SkiaSharp"]

    subgraph plateforme["FactoryTrack — plateforme"]
        ingestion["<b>FactoryTrack.Ingestion</b><br/>ASP.NET Core + gRPC<br/>idempotence, fenetre de<br/>regroupement, trilateration"]
        api["<b>FactoryTrack.Api</b><br/>ASP.NET Core + SignalR<br/>REST + hub temps reel<br/>surveillance silence & zones"]
        db[("<b>TimescaleDB / PostgreSQL</b><br/>referentiel + hypertable<br/>positions + agregat continu")]
    end

    passerelles -->|"gRPC stream<br/>MesureRssiMessage"| ingestion
    ingestion -->|"EF Core<br/>INSERT Position"| db
    ingestion -->|"SignalR client<br/>DiffuserPosition"| api
    api -->|"EF Core<br/>SELECT DISTINCT ON<br/>SELECT historique"| db
    api -->|"WebSocket<br/>PositionMiseAJour<br/>AlerteZoneEntree/Sortie<br/>EquipementSilencieux/Actif"| mobile
    mobile -->|"REST<br/>/api/equipements<br/>/api/positions/historique"| api

    classDef service fill:#1E3A5F,stroke:#0D1F35,color:#fff
    classDef stockage fill:#3D5A6C,stroke:#2A3F4D,color:#fff
    classDef externe fill:#6B7684,stroke:#4A5461,color:#fff
    classDef client fill:#2E86AB,stroke:#1A5D92,color:#fff

    class ingestion,api service
    class db stockage
    class passerelles externe
    class mobile client
```

## Choix structurants

- **gRPC en entree, SignalR en sortie.** L'ingestion recoit un flux dense et
  binaire ; la diffusion cible des clients heterogenes qui savent gerer
  WebSocket + reconnexion. Chacun a un role distinct.
- **L'ingestion parle a l'API via SignalR client**, pas en processus commun.
  L'API reste la seule proprietaire du hub. Voir ADR 0002.
- **Une seule base**, avec deux natures de tables (referentiel classique +
  hypertable serie temporelle). Pas de segregation lecture/ecriture en V1.
