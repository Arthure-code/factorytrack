# ADR 0002 — Pas de courtier de messages en V1

Statut : accepte
Date : 2026-08-15

## Contexte

Les architectures de reference pour ce type de systeme placent generalement un
courtier (RabbitMQ, Kafka) entre l'ingestion et la diffusion.

## Decision

La V1 s'en passe. L'ingestion publie ses positions via l'abstraction
`IPublicateurPositions`, implementee par un client SignalR qui appelle le hub de l'API.

## Justification

Un courtier resout le decouplage temporel, la reprise apres panne du consommateur
et la montee en charge par competition. Avec un seul producteur et un seul
consommateur, aucun de ces trois problemes ne se pose encore. L'ajouter
maintenant reviendrait a payer un conteneur, une dependance et une source de
panne supplementaires pour un benefice nul.

## Consequences

- Une position produite pendant une indisponibilite de l'API est perdue pour la
  diffusion. Elle reste ecrite en base : les clients la retrouvent par l'appel
  REST de resynchronisation.
- Le passage a un courtier consiste a ecrire une seconde implementation de
  `IPublicateurPositions`. Aucun appelant n'est modifie.

## Declencheurs de reexamen

- Plus d'une instance d'ingestion.
- Un second consommateur des positions (alertes, exports).
- Une perte de diffusion jugee inacceptable.
