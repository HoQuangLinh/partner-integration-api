using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PartnerIntegration.Application.Abstractions.Messaging;
using PartnerIntegration.Infrastructure.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace PartnerIntegration.Infrastructure.Messaging;

public class RabbitMqTransactionMessagePublisher(
    IRabbitMqConnectionManager connectionManager,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqTransactionMessagePublisher> logger)
    : ITransactionMessagePublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqOptions _options = options.Value;

    public async Task PublishAsync(
        PartnerTransactionAcceptedV1 message,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var connection = await connectionManager
                .GetOpenConnectionAsync(cancellationToken);
            var channelOptions = new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true);
            await using var channel = await connection
                .CreateChannelAsync(channelOptions, cancellationToken);

            await channel.QueueDeclareAsync(
                _options.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);
            var messageBody = JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
            var messageProperties = new BasicProperties
            {
                ContentType = "application/json",
                ContentEncoding = "utf-8",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = message.MessageId.ToString("D"),
                CorrelationId = message.CorrelationId,
                Type = message.MessageType,
                Timestamp = new AmqpTimestamp(message.AcceptedAtUtc.ToUnixTimeSeconds())
            };

            await channel.BasicPublishAsync(
                string.Empty,
                _options.QueueName,
                mandatory: true,
                messageProperties,
                messageBody,
                cancellationToken);

            logger.LogInformation(
                "Published message {MessageId} for transaction {TransactionReference} and partner {PartnerId} " +
                "with correlation {CorrelationId} to queue {QueueName} in {ElapsedMilliseconds} ms",
                message.MessageId,
                message.TransactionReference,
                message.PartnerId,
                message.CorrelationId,
                _options.QueueName,
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is RabbitMQClientException or IOException)
        {
            logger.LogError(
                exception,
                "Publishing message {MessageId} for transaction {TransactionReference} failed after " +
                "{ElapsedMilliseconds} ms with category {ErrorCategory}",
                message.MessageId,
                message.TransactionReference,
                stopwatch.ElapsedMilliseconds,
                "MessageBroker");
            throw new TransactionMessagePublishingException(exception);
        }
    }
}
