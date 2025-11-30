using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Azure;
using Azure.Messaging.EventGrid;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Source.Core;
using Source.Core.Database;

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

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Register Event Grid publisher if configuration is present
var topicEndpoint = builder.Configuration["EventGrid:TopicEndpoint"];
var topicKey = builder.Configuration["EventGrid:TopicKey"];

if (!string.IsNullOrWhiteSpace(topicEndpoint) && !string.IsNullOrWhiteSpace(topicKey))
{
    builder.Services.AddSingleton(_ =>
        new EventGridPublisherClient(new Uri(topicEndpoint), new AzureKeyCredential(topicKey)));
}

builder.Build().Run();
