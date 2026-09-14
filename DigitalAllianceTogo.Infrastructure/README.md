# DigitalAllianceTogo.Infrastructure

Couche Infrastructure : `ApplicationDbContext` + 31 classes `IEntityTypeConfiguration<T>`
(une par entité du Domain), organisées dans les mêmes sous-dossiers que `GkasGroup.Domain`.

## Choix techniques

- **PostgreSQL** via `Npgsql.EntityFrameworkCore.PostgreSQL`, comme convenu.
- **Enums stockés en `string`** (`HasConversion<string>()`) plutôt qu'en entier : plus lisible
  directement en base, et surtout plus robuste — si quelqu'un réordonne ou insère une valeur au
  milieu d'un enum C#, un stockage en `int` casserait silencieusement les données existantes.
  Le coût (un peu plus de place disque, index légèrement moins rapides) est négligeable ici.
- **`decimal(18,2)` en convention globale** via `ConfigureConventions` dans le DbContext, pour
  ne pas répéter `.HasPrecision(18,2)` sur les ~20 propriétés `decimal` du modèle. Seule
  exception : `Latitude`/`Longitude` de `PreuveLivraison`, qui ont leur propre précision GPS
  (9,6) définie explicitement dans leur configuration.
- **`StockProduit.QuantiteDisponible`** est marquée `.Ignore()` : c'est une propriété calculée
  en C#, jamais une colonne.
- **Suppressions en cascade réfléchies**, pas mises par défaut partout :
  - `Cascade` uniquement pour les relations de composition réelle (une commande possède ses
    versions, une version possède ses lignes, un devis possède ses lignes, un produit possède
    ses images/attributs, un ticket SAV possède ses diagnostics/interventions...).
  - `Restrict` pour tout ce qui touche à l'historique commercial/financier/légal (Paiement,
    Remboursement, Avoir, JournalAudit, LigneCommande vs Produit, etc.) : on ne veut jamais
    perdre une trace financière ou d'audit à cause d'une suppression en cascade accidentelle.
  - `SetNull` pour les relations optionnelles de type "assignation" (technicien, livreur,
    devis d'origine) : si l'utilisateur ou le devis est supprimé, la commande/livraison reste,
    juste sans référence.
- **Index uniques** posés partout où le diagramme implique une contrainte métier forte :
  `Email` (Utilisateur), `CodeClient` (Client), `Reference` (Produit, Commande, Devis, Paiement,
  Remboursement, Avoir, Livraison, TicketSAV), `(UtilisateurId, RoleId)`, `(RoleId, PermissionId)`,
  `(EntrepotId, ProduitId)`, `(PanierId, ProduitId)`, `(CommandeId, NumeroVersion)`.

## Ce qui n'est PAS encore géré ici (volontairement)

- La règle "un seul Panier `Actif` par utilisateur" n'est pas une contrainte SQL — elle sera
  appliquée dans la couche Application (trop spécifique pour rester portable en Fluent API pur).
- Pas encore de `DbContextFactory` pour les migrations design-time, ni de fichier
  `appsettings.json` / injection de la chaîne de connexion — ça viendra avec le projet API.
- Pas de Value Objects (ex: transformer `Email` en type dédié avec validation) — resté simple
  en `string` pour l'instant, à discuter si vous voulez durcir le modèle plus tard.

## Prochaine étape

1. ✅ Solution .NET
2. ✅ Clean Architecture (squelette)
3. ✅ Domain
4. ✅ Entités métier
5. ✅ DbContext + EF Core ← **on est ici**
6. ⬜ PostgreSQL + migrations (`dotnet ef migrations add InitialCreate`)
7. ⬜ Application / Use Cases
8. ⬜ Infrastructure (repositories, services)
9. ⬜ API (Controllers)
10. ⬜ Authentification / autorisation
