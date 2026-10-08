using Application;
using Infrastructure;
using Infrastructure.Reports;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddInfrastructure(builder.Configuration, builder.Environment)
    .AddApplication(builder.Configuration)
    .AddReportScheduling(builder.Configuration);

var host = builder.Build();
await host.RunAsync();
