using DigitalWalletApi;
using DigitalWalletCore.Exceptions;
using DigitalWalletInfrastructure.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

Log.Information("Campus Pay is starting...");

Console.WriteLine("Environment: " + builder.Environment.EnvironmentName);

Console.WriteLine(
    "Serilog Exists: " +
    builder.Configuration.GetSection("Serilog").Exists());

Console.WriteLine(
    "Connection String: " +
    builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddAppDI(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<GlobalExceptionHandler>();

app.UseHttpsRedirection();

app.UsePresentation();

app.MapGet("/config-test", (IConfiguration config) =>
{
    return Results.Ok(new
    {
        HasSerilog = config.GetSection("Serilog").Exists(),
        Environment = app.Environment.EnvironmentName
    });
});

await app.SeedDatabaseAsync();

await app.RunAsync();