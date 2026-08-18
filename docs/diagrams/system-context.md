# System context

Level 1 of the C4 model. Answers the question "who uses this and what does it
talk to?" without exposing internals.

```mermaid
flowchart TB
    operateur["👷 Operateur atelier<br/>consulte le plan sur mobile"]
    superviseur["🧑‍💼 Superviseur<br/>recoit les alertes zone"]

    factorytrack{{"<b>FactoryTrack</b><br/>RTLS indoor<br/>(back-end + client MAUI)"}}

    balises["📡 Balises BLE / UWB<br/>fixees sur equipements"]
    passerelles["📶 Passerelles radio<br/>mesurent le RSSI"]

    operateur -->|"consulte le plan<br/>tape sur un equipement"| factorytrack
    superviseur -->|"recoit alertes<br/>zones interdites"| factorytrack

    balises -.->|"emissions radio"| passerelles
    passerelles -->|"gRPC streaming<br/>MesureRssi"| factorytrack

    classDef systeme fill:#1E3A5F,stroke:#0D1F35,color:#fff,stroke-width:2px
    classDef acteur fill:#2E86AB,stroke:#1A5D92,color:#fff
    classDef materiel fill:#6B7684,stroke:#4A5461,color:#fff

    class factorytrack systeme
    class operateur,superviseur acteur
    class balises,passerelles materiel
```

## Ce que le systeme ne fait PAS

- Il n'ecoute pas les ondes radio lui-meme : ce sont les passerelles qui
  captent, FactoryTrack recoit un flux de mesures deja numerisees.
- Il ne pilote pas les equipements : il les localise, un autre systeme decide.
- Il ne gere pas l'authentification en V1 : voir README > Limitations.
