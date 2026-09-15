using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Utilisateurs.Commonds.CreateUtilisateur;
using DigitalAllianceTogo.Infrastructure.Persitence;
using DigitalAllianceTogo.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

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
});

// Register PasswordHasher
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
//
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
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

app.UseAuthorization();

app.MapControllers();

app.Run();
