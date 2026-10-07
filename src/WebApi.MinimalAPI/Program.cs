using System.Diagnostics;
using Application;
using HealthChecks.UI.Client;
using Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using WebApi.MinimalAPI;
using WebApi.MinimalAPI.Endpoints.Common;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddInfrastructure(builder.Configuration, builder.Environment)
    .AddApplication(builder.Configuration)
    .AddPresentation(builder.Configuration);

builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Conditional(static _ => Debugger.IsAttached, static writeTo => writeTo.Console());

    var appInsightsConnectionString = context.Configuration["ApplicationInsights:ConnectionString"];
    if (!Debugger.IsAttached && !string.IsNullOrEmpty(appInsightsConnectionString))
    {
        loggerConfig.WriteTo.ApplicationInsights(appInsightsConnectionString, new TraceTelemetryConverter());
    }
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("portfolio-dashboard", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "http://localhost:3001",
                "https://localhost:3001",
                "https://portfolio-web.redstone-57b2779e.spaincentral.azurecontainerapps.io"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

//
var app = builder.Build();
await app.Services.EnsureCosmosDbInitializedAsync();
app.UseCors("portfolio-dashboard");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion();
    app.MapScalarApiReference();
}
app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseOutputCache();

app.RegisterEndpoints();

app.MapHealthChecksUI();
app.MapHealthChecks("/health/json", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHealthChecks("/health");
app.Run();

