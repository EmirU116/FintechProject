using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Source.Core;
using Source.Core.Database;
using Source.Core.Eventing;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register PostgreSQL Database Context
var connectionString = builder.Configuration.GetConnectionString("PostgreSqlConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register Repositories
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ICreditCardRepository, CreditCardRepository>();

// Register Services
builder.Services.AddScoped<MoneyTransferService>();
builder.Services.AddScoped<IEventGridPublisher, EventGridPublisher>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Reduce App Insights ingestion with worker telemetry sampling
// Note: .NET isolated worker uses ApplicationInsights.WorkerService; sampling is controlled by env/appsettings.
// You can set "SamplingSettings": { "IsEnabled": true, "MaxTelemetryItemsPerSecond": 5 } via configuration if needed.
builder.Build().Run();
