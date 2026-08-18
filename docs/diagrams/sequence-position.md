# Sequence : une position bout en bout

Ce que fait le systeme quand une balise emet, depuis l'onde radio jusqu'au pixel
sur l'ecran de l'operateur.

```mermaid
sequenceDiagram
    autonumber
    participant B as Balise BLE/UWB
    participant GW as Passerelles (x4)
    participant I as Ingestion (gRPC)
    participant DB as TimescaleDB
    participant API as API (SignalR hub)
    participant M as MAUI

    B->>GW: emission radio
    GW->>I: MesureRssiMessage (stream gRPC)<br/>4 mesures pour la meme balise

    Note over I: Verifie idempotence<br/>(balise, passerelle, horodatage)
    Note over I: Regroupe par balise<br/>jusqu'a 4/4 ou expiration<br/>de FenetreRegroupementMs
    Note over I: Rejette si hors ordre<br/>(horodatage < dernier traite)

    I->>I: RSSI → distance (log-distance)<br/>Trilateration moindres carres<br/>Lissage exponentiel + garde saut

    par Persistance
        I->>DB: INSERT Position
    and Diffusion
        I->>API: DiffuserPosition(dto)<br/>via SignalR client
        API->>M: PositionMiseAJour<br/>(groupe etage-N)
    end

    Note over M: EquipementApercu<br/>met a jour X, Y, Silencieux<br/>La VuePlanUsine se redessine

    Note over API: Toutes les 5s :<br/>ServiceSurveillanceZones scanne
    API-->>DB: SELECT DISTINCT ON dernieres positions
    DB-->>API: positions courantes
    alt Transition dans zone interdite
        API->>M: AlerteZoneEntree(dto)
        Note over M: Banniere rouge + point rouge
    end
```

## Points a retenir

- **Une seule position par batch.** L'ingestion attend d'avoir un lot coherent
  avant de trilaterer ; elle ne pousse pas une position par mesure.
- **Persistance et diffusion sont paralleles.** Une panne SignalR n'empeche pas
  le stockage : le hub est reconnectable, la base est autoritative.
- **Les alertes zones sont un processus separe** qui lit l'etat courant en
  base. Le calcul de position n'a pas a savoir ou sont les zones.
