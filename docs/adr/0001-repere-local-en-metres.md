# ADR 0001 — Repere local en metres plutot que latitude/longitude

Statut : accepte
Date : 2026-08-15

## Contexte

Le systeme localise des equipements a l'interieur d'un batiment. Les frameworks
cartographiques grand public (Google Maps, Apple Maps, `Microsoft.Maui.Controls.Maps`)
travaillent en coordonnees geographiques.

## Decision

Les positions sont exprimees en metres dans un repere cartesien local, avec une
composante d'etage entiere : `(X, Y, Etage)`.

## Consequences

Positives :
- Les calculs de distance sont euclidiens, sans projection ni formule de haversine.
- La trilateration opere directement dans l'espace de mesure.
- Une precision annoncee en metres est immediatement interpretable.

Negatives :
- Les composants cartographiques terrestres sont inutilisables. Le rendu du plan
  repose sur SkiaSharp cote MAUI et SVG cote web.
- Une conversion serait necessaire pour croiser ces donnees avec une source
  geographique externe. Aucun besoin identifie a ce jour.
