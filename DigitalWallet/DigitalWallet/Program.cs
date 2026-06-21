using DigitalWalletApi;
using DigitalWalletCore.Exceptions;
using DigitalWalletInfrastructure.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAppDI(builder.Configuration);

builder.Host.UseSerilog();

Log.Logger = new LoggerConfiguration()
.ReadFrom.Configuration(builder.Configuration)
.CreateLogger();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<GlobalExceptionHandler>();

app.UseHttpsRedirection();

app.UsePresentation();
await app.SeedDatabaseAsync();

await app.RunAsync();