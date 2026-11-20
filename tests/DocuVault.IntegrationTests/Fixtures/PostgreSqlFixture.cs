using Testcontainers.PostgreSql;

namespace DocuVault.IntegrationTests.Fixtures;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("docuvault_test")
            .WithUsername("postgres")
            .WithPassword("test-password")
            .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }
}
