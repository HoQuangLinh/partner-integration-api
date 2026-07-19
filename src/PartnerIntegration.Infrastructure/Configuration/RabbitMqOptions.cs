using System.ComponentModel.DataAnnotations;

namespace PartnerIntegration.Infrastructure.Configuration;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required]
    public required string HostName { get; init; }

    [Range(1, 65_535)]

    public int Port { get; init; } = 5672;

    [Required]
    public required string UserName { get; init; }

    [Required]
    public required string Password { get; init; }

    [Required]
    public string VirtualHost { get; init; } = "/";

    [Required]
    public required string QueueName { get; init; }
}
