using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Azure;
using Azure.Messaging.EventGrid;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Source.Core;
using Source.Core.Database;
using Source.Core.Middleware;
using Functions.Middleware;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureFunctionsWebApplication(app =>
    {
        // Uncomment to enable rate limiting middleware
        // app.UseMiddleware<RateLimitMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        // Register PostgreSQL Database Context
        var connectionString = context.Configuration.GetConnectionString("PostgreSqlConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Register Repositories
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ICreditCardRepository, CreditCardRepository>();

        // Register Services
        services.AddScoped<MoneyTransferService>();

        // Register Rate Limiting (optional - configure in local.settings.json)
        var rateLimitOptions = new RateLimitOptions
        {
            Enabled = context.Configuration.GetValue<bool>("RateLimit:Enabled", false),
            MaxRequestsPerWindow = context.Configuration.GetValue<int>("RateLimit:MaxRequestsPerWindow", 100),
            WindowDuration = TimeSpan.FromMinutes(context.Configuration.GetValue<int>("RateLimit:WindowDurationMinutes", 1)),
            BurstLimit = context.Configuration.GetValue<int>("RateLimit:BurstLimit", 200)
        };
        services.AddSingleton(rateLimitOptions);
        services.AddSingleton<RateLimiter>(sp => 
            new RateLimiter(rateLimitOptions.MaxRequestsPerWindow, rateLimitOptions.WindowDuration));

        services
            .AddApplicationInsightsTelemetryWorkerService()
            .ConfigureFunctionsApplicationInsights();

        // Register Event Grid publisher if configuration is present
        var topicEndpoint = context.Configuration["EventGrid:TopicEndpoint"];
        var topicKey = context.Configuration["EventGrid:TopicKey"];

        if (!string.IsNullOrWhiteSpace(topicEndpoint) && !string.IsNullOrWhiteSpace(topicKey))
        {
            services.AddSingleton(_ =>
                new EventGridPublisherClient(new Uri(topicEndpoint), new AzureKeyCredential(topicKey)));
        }
    });

builder.Build().Run();
