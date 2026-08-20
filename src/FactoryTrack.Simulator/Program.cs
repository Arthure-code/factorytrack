using FactoryTrack.Simulator;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<OptionsSimulateur>(
    builder.Configuration.GetSection(OptionsSimulateur.Section));

builder.Services.AddHostedService<TravailleurSimulation>();

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

var hote = builder.Build();
await hote.RunAsync();
