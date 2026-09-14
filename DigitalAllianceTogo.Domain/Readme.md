# DigitalAllianceTogo.Domain

Couche Domain de la Clean Architecture — 31 entités + enums, correspondant 1:1 au
diagramme de classes final.

## Organisation

```
Entities/
  Security/    Utilisateur, Client, Adresse, Role, Permission, UtilisateurRole, RolePermission
  Catalogue/   Produit, Categorie, Marque, ImageProduit, AttributProduit
  Stock/       Entrepot, StockProduit, MouvementStock
  Devis/       Devis, LigneDevis
  Panier/      Panier, LignePanier
  Commande/    Commande, VersionCommande, LigneCommande, AdresseLivraisonCommande
  Finance/     Paiement, Remboursement, Avoir
  Livraison/   Livraison, PreuveLivraison
  SAV/         TicketSAV, Diagnostic, Intervention
  Audit/       JournalAudit
Enums/
  Enums.cs     Les 10 enums du diagramme
```

## Choix de conception

- **Aucune dépendance externe** dans ce projet (pas de `[Key]`, pas d'attributs EF Core).
  Le mapping EF Core (Fluent API) se fera entièrement dans `GkasGroup.Infrastructure`,
  pour garder le Domain pur — c'est le principe central de la Clean Architecture.
- **`StockProduit.QuantiteDisponible`** est une propriété calculée en mémoire
  (`QuantitePhysique - QuantiteReservee`), pas stockée. Il faudra la marquer
  `.Ignore()` dans la configuration EF Core.
- **`AdresseLivraisonCommande`** est volontairement dupliquée par rapport à `Adresse` :
  c'est un snapshot figé au moment de la commande, qui ne doit jamais changer même si
  le client modifie son adresse plus tard.
- **`Paiement` / `Remboursement` / `Avoir`** pointent vers `VersionCommande` (pas
  `Commande` directement), conformément à la relation `concerne` du diagramme — c'est
  volontaire pour que chaque opération financière soit rattachée à l'état exact de la
  commande au moment où elle a eu lieu.
- Les collections de navigation sont bidirectionnelles là où c'est utile pour les
  requêtes (LINQ des deux côtés), initialisées à `new List<T>()` pour éviter les
  `NullReferenceException`.
- Certaines classes référencent des entités d'autres dossiers via leur namespace complet
  (`Entities.Panier.Panier`, etc.) plutôt que des `using` en haut de fichier, pour éviter
  les ambiguïtés de nom (ex: il n'y en a pas ici, mais c'est une convention utile quand le
  modèle grandit).

## Prochaine étape (dans l'ordre convenu)

1. ✅ Solution .NET
2. ✅ Clean Architecture (squelette)
3. ✅ Domain
4. ✅ Entités métier ← **on est ici**
5. ⬜ DbContext + EF Core (`GkasGroup.Infrastructure`)
6. ⬜ PostgreSQL + migrations
7. ⬜ Application / Use Cases (CQRS ou services)
8. ⬜ Infrastructure (repositories, services externes)
9. ⬜ API (Controllers)
10. ⬜ Authentification / autorisation (JWT)

Prochaine étape logique : le `DbContext` EF Core avec toute la config Fluent API
(clés composites, cascades, index uniques, conversion des enums, `Ignore()` sur
`QuantiteDisponible`, etc.) dans `GkasGroup.Infrastructure`.
