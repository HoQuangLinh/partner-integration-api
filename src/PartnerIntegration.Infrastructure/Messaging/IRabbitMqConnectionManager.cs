using RabbitMQ.Client;

namespace PartnerIntegration.Infrastructure.Messaging;

public interface IRabbitMqConnectionManager
{
    Task<IConnection> GetOpenConnectionAsync(CancellationToken cancellationToken);
}
