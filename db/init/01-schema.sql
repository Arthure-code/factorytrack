-- Schema de reference pour la V1.
-- EF Core decrit le modele, mais la conversion en hypertable et les politiques
-- Timescale ne sont pas exprimables par le ChangeTracker : elles vivent ici.
-- Voir docs/adr/0003-schema-sql-plutot-que-migrations.md

CREATE EXTENSION IF NOT EXISTS timescaledb;

CREATE TABLE IF NOT EXISTS balises (
    "Id"                 uuid PRIMARY KEY,
    "Identifiant"        varchar(100) NOT NULL UNIQUE,
    "Technologie"        integer NOT NULL DEFAULT 0,
    "PuissanceReference" double precision NOT NULL DEFAULT -59,
    "DateModification"   timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS passerelles (
    "Id"               uuid PRIMARY KEY,
    "Identifiant"      varchar(100) NOT NULL UNIQUE,
    "X"                double precision NOT NULL,
    "Y"                double precision NOT NULL,
    "Etage"            integer NOT NULL DEFAULT 0,
    "Active"           boolean NOT NULL DEFAULT true,
    "DateModification" timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS equipements (
    "Id"               uuid PRIMARY KEY,
    "Code"             varchar(50) NOT NULL UNIQUE,
    "Nom"              varchar(200) NOT NULL,
    "Categorie"        varchar(100),
    "BaliseId"         uuid REFERENCES balises("Id") ON DELETE SET NULL,
    "Etat"             integer NOT NULL DEFAULT 0,
    "DateModification" timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS zones (
    "Id"        uuid PRIMARY KEY,
    "Nom"       varchar(200) NOT NULL,
    "Etage"     integer NOT NULL DEFAULT 0,
    "XMin"      double precision NOT NULL,
    "YMin"      double precision NOT NULL,
    "XMax"      double precision NOT NULL,
    "YMax"      double precision NOT NULL,
    "Interdite" boolean NOT NULL DEFAULT false,
    "Perimetre" boolean NOT NULL DEFAULT false
);

-- Sans migrations EF, on ajoute la colonne a la volee si la table existe deja
-- (bases anciennes qui n'ont pas ete recreees).
ALTER TABLE zones ADD COLUMN IF NOT EXISTS "Perimetre" boolean NOT NULL DEFAULT false;

CREATE TABLE IF NOT EXISTS machines_fixes (
    "Id"       uuid PRIMARY KEY,
    "Code"     varchar(50) NOT NULL UNIQUE,
    "Nom"      varchar(200) NOT NULL,
    "Etage"    integer NOT NULL DEFAULT 0,
    "X"        double precision NOT NULL,
    "Y"        double precision NOT NULL,
    "Largeur"  double precision NOT NULL,
    "Hauteur"  double precision NOT NULL
);

-- Serie temporelle. La cle primaire inclut l'horodatage : Timescale exige que la
-- colonne de partitionnement fasse partie de toute contrainte unique.
CREATE TABLE IF NOT EXISTS positions (
    "BaliseId"          uuid NOT NULL,
    "BaliseIdentifiant" varchar(100) NOT NULL,
    "X"                 double precision NOT NULL,
    "Y"                 double precision NOT NULL,
    "Etage"             integer NOT NULL DEFAULT 0,
    "Precision"         double precision NOT NULL,
    "Technologie"       integer NOT NULL DEFAULT 0,
    "NombreAncres"      integer NOT NULL,
    "Horodatage"        timestamptz NOT NULL,
    PRIMARY KEY ("BaliseId", "Horodatage")
);

SELECT create_hypertable('positions', 'Horodatage',
                         chunk_time_interval => INTERVAL '1 day',
                         if_not_exists => TRUE);

CREATE INDEX IF NOT EXISTS ix_positions_etage_horodatage
    ON positions ("Etage", "Horodatage" DESC);

-- Agregation continue : le mode replay lit ceci plutot que les positions brutes.
CREATE MATERIALIZED VIEW IF NOT EXISTS positions_agregees_minute
WITH (timescaledb.continuous) AS
SELECT
    "BaliseId",
    "BaliseIdentifiant",
    time_bucket(INTERVAL '1 minute', "Horodatage") AS minute,
    avg("X")         AS x_moyen,
    avg("Y")         AS y_moyen,
    avg("Precision") AS precision_moyenne,
    count(*)         AS nombre_mesures
FROM positions
GROUP BY "BaliseId", "BaliseIdentifiant", minute
WITH NO DATA;

SELECT add_continuous_aggregate_policy('positions_agregees_minute',
    start_offset => INTERVAL '1 hour',
    end_offset   => INTERVAL '1 minute',
    schedule_interval => INTERVAL '1 minute',
    if_not_exists => TRUE);

-- Les positions brutes ne servent qu'au court terme : l'agregat prend le relais.
SELECT add_retention_policy('positions', INTERVAL '7 days', if_not_exists => TRUE);
