# GkasGroup.Application

Couche Application en CQRS (MediatR) — logique métier, orchestration, validation.
Aucune dépendance à EF Core, PostgreSQL ou ASP.NET Core : uniquement des interfaces.

## Structure

```
Common/
  Interfaces/     IApplicationDbContext, IPasswordHasher, ICurrentUserService
  Behaviours/     ValidationBehaviour, LoggingBehaviour, UnhandledExceptionBehaviour
  Exceptions/     NotFoundException, ValidationException, ConflictException
  Models/         PaginatedList<T>

Utilisateurs/           <- module de référence, à dupliquer pour les autres
  Dtos/
    UtilisateurDto.cs
  Commands/
    CreateUtilisateur/  Command + Handler + Validator
    UpdateUtilisateur/  Command + Handler + Validator
    DeleteUtilisateur/  Command + Handler
    AssignerRole/       Command + Handler + Validator
    RetirerRole/        Command + Handler
  Queries/
    GetUtilisateurById/ Query + Handler
    GetUtilisateurs/    Query paginée + Handler + Validator
```

## Pipeline MediatR (ordre d'exécution pour CHAQUE Command/Query)

```
Requête entrante
   │
   ▼
UnhandledExceptionBehaviour   (filet de sécurité, logue toute exception imprévue)
   │
   ▼
LoggingBehaviour              (trace qui a fait quoi)
   │
   ▼
ValidationBehaviour           (FluentValidation — bloque ICI si invalide,
   │                           le Handler n'est jamais atteint)
   ▼
Handler                       (logique métier réelle)
   │
   ▼
Réponse
```

## Pourquoi ce découpage (Command+Handler dans le même fichier, Validator séparé)

C'est la convention du template "Clean Architecture" de Jason Taylor, devenu un
standard de facto dans l'écosystème .NET — un dossier par cas d'usage, avec tout
ce qui le concerne à l'intérieur. Ça facilite la navigation : pour comprendre
"comment on assigne un rôle", tu ouvres `Commands/AssignerRole/` et tout y est.

## Ce qui reste à faire avant de pouvoir compiler l'API

- **`ICurrentUserService`** n'a pas encore d'implémentation concrète : ça viendra
  dans `GkasGroup.Api`, car cette classe a besoin de lire le `HttpContext` /
  le token JWT — une dépendance ASP.NET Core qui n'a pas sa place dans
  Application ni Infrastructure.
- Pas encore de gestion centralisée des erreurs HTTP (middleware qui transforme
  `NotFoundException` → 404, `Application.Common.Exceptions.ValidationException` → 400,
  `ConflictException` → 409) — ce sera fait dans l'API avec `IExceptionHandler` (ASP.NET Core 8).

## Prochaine étape

1. ✅ Solution .NET
2. ✅ Clean Architecture (squelette)
3. ✅ Domain
4. ✅ Entités métier
5. ✅ DbContext + EF Core
6. ✅ PostgreSQL + migrations (fait par toi)
7. ✅ Application / Use Cases ← **module Utilisateur fait, à dupliquer pour les autres**
8. ✅ Infrastructure branchée sur les interfaces Application
9. ⬜ API (Controllers + Program.cs + middleware d'erreurs)
10. ⬜ Authentification / autorisation (JWT)

Prochaine étape logique : le projet `GkasGroup.Api` — Controllers minces qui
envoient juste les Commands/Queries à MediatR, `Program.cs` qui branche tout
(`AddApplicationServices()` + `AddInfrastructureServices()`), et le middleware
de gestion d'erreurs global.
