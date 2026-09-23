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
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
// Récupération de la chaîne de connexion PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
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

// Register PasswordHasher
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
builder.Services.Configure<JwtSettings>(jwtSection);
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// Configure JWT Authentication
var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings();

if (string.IsNullOrEmpty(jwtSettings.Key))
{
    throw new InvalidOperationException("JWT Key is not configured. Set 'Jwt:Key' in appsettings.json");
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

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
//
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("ServerSide API")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});
// Redirect root URL to Scalar documentation
app.MapGet("/", () => Results.Redirect("/scalar/v1"))
    .ExcludeFromDescription();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
