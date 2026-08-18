using FactoryTrack.Api.Endpoints;
using FactoryTrack.Api.Hubs;
using FactoryTrack.Api.Services;
using FactoryTrack.Contracts;
using FactoryTrack.Domain.Options;
using FactoryTrack.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((contexte, configuration) =>
    configuration.ReadFrom.Configuration(contexte.Configuration));

builder.Services.Configure<OptionsPositionnement>(
    builder.Configuration.GetSection(OptionsPositionnement.Section));

builder.Services.AjouterInfrastructure(builder.Configuration);

builder.Services.AddSignalR();
builder.Services.AddHostedService<ServiceSurveillanceSilence>();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString(InjectionDependances.CHAINE_CONNEXION)!);

const string POLITIQUE_CORS = "ClientsFactoryTrack";

// AllowAnyOrigin est incompatible avec AllowCredentials : SignalR exige les
// identifiants, donc les origines doivent etre listees explicitement.
builder.Services.AddCors(options =>
    options.AddPolicy(POLITIQUE_CORS, politique =>
        politique
            .WithOrigins(builder.Configuration.GetSection("Cors:Origines").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors(POLITIQUE_CORS);

app.MapperEquipements();
app.MapperPositions();
app.MapperReferentiel();

app.MapHub<PositionHub>(NomsHub.Chemin);
app.MapHealthChecks("/health");

app.Run();

/// <summary>Expose pour les tests d'integration (WebApplicationFactory).</summary>
public partial class Program { }
