# ADR 0003 — Schema SQL versionne plutot que migrations EF Core en V1

Statut : accepte
Date : 2026-08-15

## Contexte

EF Core decrit le modele relationnel, mais ne connait pas les objets propres a
TimescaleDB : `create_hypertable`, agregations continues, politiques de retention.

## Decision

Le schema est defini par des scripts SQL sous `db/init/`, executes au premier
demarrage du conteneur PostgreSQL. EF Core est configure en accord avec ce
schema mais n'en est pas la source de verite en V1.

## Consequences

Positives :
- `docker compose up` produit une base complete, hypertable et politiques comprises.
- Le SQL Timescale est lisible tel quel, sans etre noye dans un `migrationBuilder.Sql`.

Negatives :
- Le modele EF et le SQL peuvent diverger silencieusement. Les tests d'integration
  sur une vraie base (Testcontainers) sont la garde-fou prevue en V2.
- Pas de chemin de migration incrementale : la base se recree.

## Evolution prevue

V2 : bascule vers des migrations EF Core, la conversion en hypertable etant
portee par une migration SQL brute dediee.
