using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TabulaWMS.Data;

namespace TabulaWMS.Tests.Integration;

public class TenantProvisioningAndLoginTests : IClassFixture<IntegrationTestDatabaseFixture>
{
    private const string SystemAdminEmail = "iuri@velostack.com";
    private const string SystemAdminPassword = "Password12345!";
    private const string TenantAdminPassword = "Admin123!";

    private readonly IntegrationTestDatabaseFixture _fixture;
    private readonly HttpClient _client;
    private readonly string _connectionString;

    public TenantProvisioningAndLoginTests(IntegrationTestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
        _connectionString = fixture.ConnectionString;
    }

    [Fact]
    public async Task SystemAdmin_CanLogIn()
    {
        var token = await LoginAsSystemAdminAsync();
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task SystemAdmin_CanCreateTenant()
    {
        var tenant = CreateTenantCredentials();
        await ProvisionTenantAsSystemAdminAsync(tenant);

        await AssertTenantSchemaStructureAsync(tenant.TenantId);

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TabulaContext>();
            var dirEntry = await db.TenantUserDirectories
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == tenant.Email.ToLowerInvariant());

            Assert.NotNull(dirEntry);
            Assert.Equal(tenant.TenantId, dirEntry.TenantId);
        }
    }

    [Fact]
    public async Task NewlyCreatedTenantAdmin_CanLogIn()
    {
        var tenant = CreateTenantCredentials();
        await ProvisionTenantAsSystemAdminAsync(tenant);

        using var tenantLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = tenant.Email,
            Password = tenant.Password
        });

        Assert.Equal(HttpStatusCode.OK, tenantLoginResponse.StatusCode);

        var tenantLoginBody = await tenantLoginResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(tenantLoginBody.GetProperty("token").GetString()));
        Assert.Equal("Admin", tenantLoginBody.GetProperty("role").GetString());
    }

    private async Task<string> LoginAsSystemAdminAsync()
    {
        using var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = SystemAdminEmail,
            Password = SystemAdminPassword
        });

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"System admin login failed ({response.StatusCode}): {await response.Content.ReadAsStringAsync()}");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("System admin login returned no token.");
    }

    private async Task ProvisionTenantAsSystemAdminAsync(TenantCredentials tenant)
    {
        var token = await LoginAsSystemAdminAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/tenants/provision")
        {
            Content = JsonContent.Create(new
            {
                TenantId = tenant.TenantId,
                CompanyName = "Test Logistics",
                AdminEmail = tenant.Email,
                AdminPassword = tenant.Password,
                AdminName = "Test Admin"
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _client.SendAsync(request);
        Assert.True(
            response.IsSuccessStatusCode,
            $"Tenant provisioning failed ({response.StatusCode}): {await response.Content.ReadAsStringAsync()}");
    }

    private static TenantCredentials CreateTenantCredentials()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return new TenantCredentials(
            $"tenant_{suffix}",
            $"admin-{suffix}@example.com",
            TenantAdminPassword);
    }

    private async Task AssertTenantSchemaStructureAsync(string schemaName)
    {
        string[] expectedTables =
        [
            "AspNetRoles",
            "AspNetRoleClaims",
            "AspNetUsers",
            "AspNetUserClaims",
            "AspNetUserLogins",
            "AspNetUserRoles",
            "AspNetUserTokens",
            "Products"
        ];

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = @schema
              AND table_type = 'BASE TABLE';
            """;
        cmd.Parameters.AddWithValue("schema", schemaName);

        var actualTables = new HashSet<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            actualTables.Add(reader.GetString(0));
        }

        var missingTables = expectedTables.Except(actualTables).ToArray();
        Assert.True(
            missingTables.Length == 0,
            $"Expected PostgreSQL schema '{schemaName}' to contain tables: {string.Join(", ", missingTables)}.");
    }

    private sealed record TenantCredentials(string TenantId, string Email, string Password);
}