using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace TabulaWMS.Tests.Integration;

public sealed class IntegrationTestDatabaseFixture : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory = new();

    public HttpClient Client { get; private set; } = null!;
    public string ConnectionString { get; private set; } = null!;
    public IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        try
        {
            Client = _factory.CreateClient();

            using var scope = _factory.Services.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            ConnectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Missing test DefaultConnection");
        }
        catch (Exception startupError)
        {
            try
            {
                await DisposeAsync();
            }
            catch (Exception cleanupError)
            {
                throw new AggregateException(
                    "Test database startup and cleanup both failed.",
                    startupError,
                    cleanupError);
            }

            throw;
        }
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        try
        {
            await _factory.DisposeAsync();
        }
        finally
        {
            await _factory.DropTestDatabaseAsync();
        }
    }
}

internal sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private string? _databaseName;
    private string? _testConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            if (_testConnectionString is null)
            {
                CreateTestDatabase(context, configurationBuilder);
            }

            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _testConnectionString,
                ["TestDatabase:ApplyMigrationsOnStartup"] = "true"
            });
        });
    }

    public async Task DropTestDatabaseAsync()
    {
        if (_databaseName is null || _testConnectionString is null)
        {
            return;
        }

        var adminConnection = new NpgsqlConnectionStringBuilder(_testConnectionString)
        {
            Database = "postgres"
        };

        await using var connection = new NpgsqlConnection(adminConnection.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE);";
        await command.ExecuteNonQueryAsync();
    }

    private void CreateTestDatabase(
        WebHostBuilderContext context,
        IConfigurationBuilder configurationBuilder)
    {
        var sourceConfiguration = new ConfigurationBuilder()
            .SetBasePath(context.HostingEnvironment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets(typeof(Program).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var sourceConnectionString = configurationBuilder.Build()
            .GetConnectionString("DefaultConnection")
            ?? sourceConfiguration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Missing ConnectionStrings:DefaultConnection for creating the test database.");

        _databaseName = $"tabulawms_test_{Guid.NewGuid():N}";

        var adminConnection = new NpgsqlConnectionStringBuilder(sourceConnectionString)
        {
            Database = "postgres"
        };

        using (var connection = new NpgsqlConnection(adminConnection.ConnectionString))
        {
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{_databaseName}\";";
            command.ExecuteNonQuery();
        }

        _testConnectionString = new NpgsqlConnectionStringBuilder(sourceConnectionString)
        {
            Database = _databaseName
        }.ConnectionString;
    }
}
