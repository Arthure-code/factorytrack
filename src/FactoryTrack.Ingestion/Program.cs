using FactoryTrack.Domain.Interfaces;
using FactoryTrack.Domain.Options;
using FactoryTrack.Domain.Positionnement;
using FactoryTrack.Infrastructure;
using FactoryTrack.Ingestion.Services;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((contexte, configuration) =>
    configuration.ReadFrom.Configuration(contexte.Configuration));

builder.Services.Configure<OptionsPositionnement>(
    builder.Configuration.GetSection(OptionsPositionnement.Section));

builder.Services.AjouterInfrastructure(builder.Configuration);

builder.Services.AddSingleton<ServicePositionnement>(fournisseur =>
    new ServicePositionnement(fournisseur.GetRequiredService<IOptions<OptionsPositionnement>>().Value));

builder.Services.AddSingleton<IGardeIdempotence, GardeIdempotence>();
builder.Services.AddSingleton<GardeHorsOrdre>();
builder.Services.AddScoped<IPublicateurPositions, PublicateurSignalR>();

builder.Services.AddGrpc();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString(InjectionDependances.CHAINE_CONNEXION)!);

var app = builder.Build();

app.MapGrpcService<ServiceIngestionGrpc>();
app.MapHealthChecks("/health");
app.MapGet("/", () => "FactoryTrack Ingestion - point d'entree gRPC.");

app.Run();

/// <summary>Expose pour les tests d'integration (WebApplicationFactory).</summary>
public partial class Program { }
