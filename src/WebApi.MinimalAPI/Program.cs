using Application;
using HealthChecks.UI.Client;
using Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using WebApi.MinimalAPI;
using WebApi.MinimalAPI.Endpoints.Common;
using WebApi.MinimalAPI.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddInfrastructure(builder.Configuration, builder.Environment)
    .AddApplication(builder.Configuration)
    .AddPresentation(builder.Configuration);

builder.AddSerilogLogging();

var app = builder.Build();
await app.Services.EnsureCosmosDbInitializedAsync();
app.UseCors(WebApi.MinimalAPI.DependencyInjection.CorsPolicyName);

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

