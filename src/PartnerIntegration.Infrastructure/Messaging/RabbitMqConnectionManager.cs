using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PartnerIntegration.Infrastructure.Configuration;
using RabbitMQ.Client;

namespace PartnerIntegration.Infrastructure.Messaging;

public class RabbitMqConnectionManager(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConnectionManager> logger)
    : IRabbitMqConnectionManager, IAsyncDisposable
{
    private readonly ConnectionFactory _connectionFactory = CreateConnectionFactory(options.Value);
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }

            _connection = await _connectionFactory
                .CreateConnectionAsync(cancellationToken);
            logger.LogInformation(
                "Connected to RabbitMQ at {RabbitMqHost}:{RabbitMqPort} using virtual host {RabbitMqVirtualHost}",
                _connectionFactory.HostName,
                _connectionFactory.Port,
                _connectionFactory.VirtualHost);

            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _connectionLock.Dispose();
    }

    private static ConnectionFactory CreateConnectionFactory(RabbitMqOptions options) =>
        new()
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            ClientProvidedName = "partner-integration-api"
        };
}
