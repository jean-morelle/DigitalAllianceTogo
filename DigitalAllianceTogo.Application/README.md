# Module Produits

Deuxième module CQRS complet, sur le même schéma que Utilisateurs.

## Commands
- `CreerProduit` : vérifie unicité de la Reference + existence Categorie/Marque
- `ModifierProduit` : Reference volontairement NON modifiable (identifiant métier stable)
- `SupprimerProduit` : échouera en base (contrainte Restrict) si le produit a du stock/des ventes
- `AjouterImage` / `SupprimerImage` : gère la règle "une seule image principale par produit"
- `AjouterAttribut` / `SupprimerAttribut`

## Queries
- `GetProduitById` → `ProduitDetailDto` (avec images + attributs)
- `GetProduits` → `ProduitDto` paginé, avec recherche + filtres Categorie/Marque/Actif
- `GetCategories` / `GetMarques` (dans leurs propres dossiers `Categories/` et `Marques/`) :
  listes simples non paginées, pour peupler des `<select>` côté front — PAS un vrai
  module de gestion. Un vrai CRUD Categorie/Marque (avec Commands Create/Update/Delete)
  reste à faire si vous voulez gérer le catalogue de catégories depuis l'API plutôt
  qu'en base directement.

## Choix API notable

Contrairement à `UtilisateursController` (entièrement protégé), `ProduitsController`
a sa lecture (`GET`) en `[AllowAnonymous]` : un catalogue produits doit être consultable
par un visiteur non connecté sur un site e-commerce. Seules les mutations (POST/PUT/DELETE)
exigent un rôle `Admin` ou `Catalogue`.

Les endpoints `GET /api/categories` et `GET /api/marques` sont exposés depuis
`ProduitsController` via une route absolue (`~/api/categories`) — un raccourci pragmatique
en attendant un vrai `CategoriesController`/`MarquesController` dédié. À corriger si ces
deux entités reçoivent un jour leur propre gestion complète (upload d'image de bannière,
réordonnancement, etc.).
