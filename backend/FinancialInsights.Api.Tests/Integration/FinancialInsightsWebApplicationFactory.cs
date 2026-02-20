using FinancialInsights.Api.Data;
using FinancialInsights.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FinancialInsights.Api.Tests.Integration;

public sealed class FinancialInsightsWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;

    private bool _startAttempted;
    private bool _postgresAvailable;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        EnsureContainerStarted();
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["Akahu:BaseUrl"] = "https://api.akahu.io/v1",
                ["Akahu:AppToken"] = "test-token"
            };

            if (_postgresAvailable)
            {
                values["ConnectionStrings:Default"] = _postgresContainer!.GetConnectionString();
            }

            configBuilder.AddInMemoryCollection(values);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(AppDbContext));

            if (_postgresAvailable)
            {
                ValidateTestDatabaseConnectionString(_postgresContainer!.GetConnectionString());
                services.AddDbContext<AppDbContext>(options => options.UseNpgsql(_postgresContainer!.GetConnectionString()));
            }
            else
            {
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("fi-tests"));
            }

            services.AddScoped<IAkahuClient, FakeAkahuClient>();
            services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        });
    }

    public Task InitializeAsync()
    {
        EnsureContainerStarted();
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_postgresAvailable)
        {
            await _postgresContainer!.DisposeAsync();
        }
    }

    private void EnsureContainerStarted()
    {
        if (_startAttempted)
        {
            return;
        }

        _startAttempted = true;

        try
        {
            _postgresContainer = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("financial_insights_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            _postgresContainer.StartAsync().GetAwaiter().GetResult();
            _postgresAvailable = true;
        }
        catch
        {
            _postgresAvailable = false;
        }
    }

    private static void ValidateTestDatabaseConnectionString(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database?.Trim();

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("Test database name is missing from the connection string.");
        }

        if (string.Equals(databaseName, "financial_insights", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Refusing to run tests against the live financial_insights database.");
        }

        if (!databaseName.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Refusing to run tests against non-test database '{databaseName}'.");
        }
    }

    private sealed class FakeAkahuClient : IAkahuClient
    {
        public Task<IReadOnlyList<AkahuAccount>> GetAccountsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<AkahuAccount>>([]);

        public Task<IReadOnlyList<AkahuTransaction>> GetTransactionsAsync(
            string accountId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<AkahuTransaction>>([]);
    }
}
