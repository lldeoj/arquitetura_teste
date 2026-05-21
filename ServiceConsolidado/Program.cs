using ServiceConsolidado;
using Microsoft.EntityFrameworkCore;
using ServiceLancamentos.Data;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

var defaultConnection = builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? builder.Configuration["DataBase:ConnectionString"]
    ?? Environment.GetEnvironmentVariable("CONNECTION_STRING");

if (string.IsNullOrWhiteSpace(defaultConnection))
    throw new InvalidOperationException("Connection string não configurada (DataBase:ConnectionString).");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(defaultConnection));

// register application services (rabbitmq, repositories, domain services)
builder.Services.AddApplicationServices(builder.Configuration);

// Register Worker as hosted service (will be activated by DI)
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// Ensure database exists
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxRetries = 10;
    var delay = TimeSpan.FromSeconds(2);
    bool connected = false;

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            if (await db.Database.CanConnectAsync())
            {
                logger.LogInformation("Successfully connected to the database on attempt {Attempt}.", attempt);
                db.Database.EnsureCreated();
                connected = true;
                break;
            }
            else
            {
                logger.LogInformation("Attempt {Attempt}/{MaxRetries}: Could not connect to database.", attempt, maxRetries);
            }
        }
        catch (System.Net.Sockets.SocketException ex) when (attempt < maxRetries)
        {
            logger.LogWarning(ex, "Attempt {Attempt}/{MaxRetries}: DNS or network error while connecting to database; retrying after {Delay}.", attempt, maxRetries, delay);
        }
        catch (Npgsql.NpgsqlException ex) when (attempt < maxRetries)
        {
            logger.LogWarning(ex, "Attempt {Attempt}/{MaxRetries}: Postgres not ready or connection refused; retrying after {Delay}.", attempt, maxRetries, delay);
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            logger.LogWarning(ex, "Attempt {Attempt}/{MaxRetries}: Unexpected error while connecting to database; retrying after {Delay}.", attempt, maxRetries, delay);
        }

        if (attempt < maxRetries)
        {
            await Task.Delay(delay);
            delay = TimeSpan.FromSeconds(Math.Min(30, delay.TotalSeconds * 2)); // backoff exponencial
        }
    }

    if (!connected)
    {
        logger.LogError("Failed to connect to the database after {MaxRetries} attempts.", maxRetries);
        throw new InvalidOperationException("Could not connect to the database.");
    }
}

await host.RunAsync();
