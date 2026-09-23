using DigitalAllianceTogo.Application.Common.Behaviours;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Utilisateurs.Commands.CreateUtilisateur;
using DigitalAllianceTogo.Common;
using DigitalAllianceTogo.Infrastructure.Auth;
using DigitalAllianceTogo.Infrastructure.Persistence;
using DigitalAllianceTogo.Infrastructure.Services;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
// Récupération de la chaîne de connexion PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection n'est pas configurée.");
// Add services to the container.
// Configuration d'Entity Framework Core avec PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

// Register ApplicationDbContext as IApplicationDbContext for handlers
builder.Services.AddScoped<IApplicationDbContext>(provider => 
    provider.GetRequiredService<ApplicationDbContext>());

// Register MediatR - scans for handlers in the main entry assembly and all referenced assemblies
builder.Services.AddMediatR(config =>
{
    // Register from Application namespace
    config.RegisterServicesFromAssemblyContaining<CreateUtilisateurCommand>();

    // Pipeline : UnhandledException (le plus externe) -> Logging -> Validation -> handler
    config.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));
    config.AddOpenBehavior(typeof(LoggingBehaviour<,>));
    config.AddOpenBehavior(typeof(ValidationBehaviour<,>));
});

// Enregistre tous les validateurs FluentValidation de la couche Application
builder.Services.AddValidatorsFromAssemblyContaining<CreateUtilisateurCommand>();

// Utilisateur courant (lu depuis les claims du JWT), utilisé par LoggingBehaviour
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Journal d'audit (écrit dans la même transaction que l'opération métier)
builder.Services.AddScoped<IAuditService, AuditService>();

// Annulation automatique des commandes impayées (toutes les heures)
builder.Services.AddHostedService<ExpirationCommandesService>();

// Notifications client : envoi par e-mail en tâche de fond (section « Email », inactif si non configurée)
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.AddSingleton<IEnvoiEmail, EnvoiEmailSmtp>();
builder.Services.AddHostedService<EnvoiNotificationsService>();

// Register PasswordHasher
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// Fichiers de preuve (paiement, livraison) : sur disque, hors wwwroot, servis uniquement via l'API
builder.Services.AddSingleton<IStockageFichiers>(new StockageFichiersLocal(
    builder.Configuration["Fichiers:Dossier"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "fichiers")));

var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
builder.Services.Configure<JwtSettings>(jwtSection);
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// Configure JWT Authentication
var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings();

if (string.IsNullOrEmpty(jwtSettings.Key))
{
    throw new InvalidOperationException("JWT Key is not configured. Set 'Jwt:Key' in appsettings.json");
}

// Hors développement : jamais la clé d'exemple d'appsettings.json (n'importe qui pourrait forger un jeton Admin)
if (!builder.Environment.IsDevelopment()
    && (jwtSettings.Key.StartsWith("your-super-secret", StringComparison.Ordinal) || Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32))
{
    throw new InvalidOperationException("Jwt:Key doit être une clé secrète propre à la production (32 caractères minimum).");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Traduit les exceptions applicatives (NotFound, Validation, Conflict, Unauthorized) en ProblemDetails
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddLimitationDebit();

// Derrière le proxy HTTPS (Caddy) : vraie IP du client (limitation de débit, journal d'audit) et schéma https.
// Le proxy est le seul point d'entrée (l'API n'est pas exposée) : on accepte ses en-têtes quel que soit son réseau.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers()
    // Enums en texte dans le JSON ("source": "TikTok" plutôt que 4) : lisible et stable côté front
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
//
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseForwardedHeaders();

// Production : le schéma de la base suit la version déployée (migrations appliquées au démarrage)
if (app.Configuration.GetValue<bool>("Base:MigrerAuDemarrage"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
}

// Rôles métier + administrateur initial (idempotent, ne crée que ce qui manque)
await DonneesInitiales.InitialiserAsync(app.Services);

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
// Documentation de l'API : en développement seulement (elle décrit toutes les routes internes)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("ServerSide API")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
    // Redirect root URL to Scalar documentation
    app.MapGet("/", () => Results.Redirect("/scalar/v1"))
        .ExcludeFromDescription();

    // En production, le HTTPS est assuré par le proxy
    app.UseHttpsRedirection();
}

// Surveillance : l'API répond et la base est joignable
app.MapGet("/api/sante", async (ApplicationDbContext db, CancellationToken ct) =>
        await db.Database.CanConnectAsync(ct) ? Results.Ok(new { statut = "ok" }) : Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
    .ExcludeFromDescription();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
