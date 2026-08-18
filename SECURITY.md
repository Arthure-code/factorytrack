# Politique de securite

## Portee

FactoryTrack est un projet portefeuille personnel. Il n'est pas deploye en
production, aucun utilisateur reel ne depend de lui. Le code est fourni
sous licence MIT (voir [`LICENSE`](LICENSE)).

## Versions supportees

Seule la branche par defaut (`main`) recoit des correctifs.

## Signaler une vulnerabilite

Si vous decouvrez une faille dans ce depot :

1. **Ne pas** ouvrir une issue publique decrivant l'exploitation.
2. Utiliser [GitHub Security Advisories](../../security/advisories/new) pour
   un rapport prive : la vulnerabilite est visible uniquement du proprietaire
   du depot jusqu'a divulgation coordonnee.
3. Alternative : contact direct par la page GitHub du proprietaire.

Un accuse de reception est envoye sous 7 jours. Comme le projet n'est pas
critique, aucun SLA de correction n'est garanti.

## Ce qui est deja en place

- **Secret scanning** actif (defaut GitHub sur repos publics)
- **Push protection** contre les secrets accidentels
- **Dependabot** : alertes CVE + PRs de mise a jour hebdomadaires
- **CodeQL** : analyse statique C# a chaque push + hebdomadaire
- **Trivy** : scan des images Docker (HIGH/CRITICAL bloquants)
- **SonarCloud** : quality gate + smells de securite
- **`dotnet list package --vulnerable`** : audit des dependances a chaque CI

## Limites assumees (V1 portefeuille)

Ces points sont documentes dans le [README](README.md#limitations) et
resteraient a corriger avant tout deploiement reel :

- Pas d'authentification (JWT/OIDC) sur l'API ni le hub SignalR
- Le hub `DiffuserPosition` accepte n'importe quel client
- Mots de passe de dev en clair dans `docker-compose.yml` (a passer via
  Key Vault ou variables d'environnement pour tout deploiement)
- CORS ouvert aux origines de developpement local
- HTTP non chiffre autorise sur Android (usesCleartextTraffic) pour tests
  contre `10.0.2.2`
