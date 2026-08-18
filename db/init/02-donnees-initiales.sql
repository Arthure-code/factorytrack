-- Jeu minimal permettant a la V1 de fonctionner des le premier demarrage.
-- Quatre passerelles aux coins d'une usine de 60 m x 40 m, geometrie non colineaire.

INSERT INTO passerelles ("Id", "Identifiant", "X", "Y", "Etage", "Active")
VALUES
    ('11111111-1111-1111-1111-111111111101', 'GW-01',  0,  0, 0, true),
    ('11111111-1111-1111-1111-111111111102', 'GW-02', 60,  0, 0, true),
    ('11111111-1111-1111-1111-111111111103', 'GW-03', 60, 40, 0, true),
    ('11111111-1111-1111-1111-111111111104', 'GW-04',  0, 40, 0, true)
ON CONFLICT ("Identifiant") DO NOTHING;

INSERT INTO balises ("Id", "Identifiant", "Technologie", "PuissanceReference")
SELECT
    ('22222222-2222-2222-2222-' || lpad(i::text, 12, '0'))::uuid,
    'TAG-' || lpad(i::text, 3, '0'),
    CASE WHEN i % 5 = 0 THEN 1 ELSE 0 END,  -- une balise sur cinq en UWB
    -59
FROM generate_series(1, 20) AS i
ON CONFLICT ("Identifiant") DO NOTHING;

INSERT INTO equipements ("Id", "Code", "Nom", "Categorie", "BaliseId", "Etat")
SELECT
    ('33333333-3333-3333-3333-' || lpad(i::text, 12, '0'))::uuid,
    'EQ-' || lpad(i::text, 3, '0'),
    CASE (i % 4)
        WHEN 0 THEN 'Chariot elevateur ' || i
        WHEN 1 THEN 'Palette outillage ' || i
        WHEN 2 THEN 'Chariot de pieces ' || i
        ELSE 'Poste mobile ' || i
    END,
    CASE (i % 4)
        WHEN 0 THEN 'Manutention'
        WHEN 1 THEN 'Outillage'
        WHEN 2 THEN 'Logistique'
        ELSE 'Production'
    END,
    ('22222222-2222-2222-2222-' || lpad(i::text, 12, '0'))::uuid,
    0
FROM generate_series(1, 20) AS i
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO zones ("Id", "Nom", "Etage", "XMin", "YMin", "XMax", "YMax", "Interdite")
VALUES
    ('44444444-4444-4444-4444-444444444401', 'Quai de chargement', 0,  0,  0, 15, 12, false),
    ('44444444-4444-4444-4444-444444444402', 'Zone de production', 0, 15,  5, 50, 35, false),
    ('44444444-4444-4444-4444-444444444403', 'Local electrique',   0, 52, 32, 60, 40, true)
ON CONFLICT DO NOTHING;
