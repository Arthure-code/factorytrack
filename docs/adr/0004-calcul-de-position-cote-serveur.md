# ADR 0004 — Le calcul de position se fait cote serveur

Statut : accepte
Date : 2026-08-15

## Contexte

Deux topologies sont possibles avec du Bluetooth :

1. Le client mobile scanne des balises fixes et calcule sa propre position.
2. Des passerelles fixes ecoutent des balises mobiles et transmettent les mesures
   a un serveur qui calcule.

## Decision

Topologie 2. Les balises sont sur les equipements, les passerelles sont fixes,
le calcul appartient au service de positionnement.

## Justification

La topologie 1 ne localise que l'appareil qui scanne — donc pas les equipements,
qui sont l'objet du systeme. Elle exige aussi qu'un appareil soit present sur
place, ce qui interdit la consultation a distance.

## Consequences

- N'importe quel client devient un simple afficheur : mobile, web, ou distant.
- La logique de positionnement est testable unitairement, sans appareil ni radio.
- Le serveur devient le point unique de defaillance du calcul. Compense par les
  health checks et la reprise de connexion.
