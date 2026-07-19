using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PartnerIntegration.Application.Abstractions.ExternalServices;
using PartnerIntegration.Application.Abstractions.Messaging;

namespace PartnerIntegration.IntegrationTests;

public class PartnerIntegrationApiFactory : WebApplicationFactory<Program>
{
    public StubPartnerVerificationService VerificationService { get; } = new();

    public CapturingMessagePublisher MessagePublisher { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPartnerVerificationService>();
            services.RemoveAll<ITransactionMessagePublisher>();
            services.AddSingleton<IPartnerVerificationService>(VerificationService);
            services.AddSingleton<ITransactionMessagePublisher>(MessagePublisher);
        });
    }

    public class StubPartnerVerificationService : IPartnerVerificationService
    {
        public bool IsPartnerVerified { get; set; } = true;

        public bool SimulateUnavailableVerification { get; set; }

        public int CallCount { get; private set; }

        public Task<bool> VerifyAsync(
            string partnerId,
            CancellationToken cancellationToken)
        {
            CallCount++;

            if (SimulateUnavailableVerification)
            {
                throw new PartnerVerificationUnavailableException();
            }

            return Task.FromResult(IsPartnerVerified);
        }

    }

    public class CapturingMessagePublisher : ITransactionMessagePublisher
    {
        public bool SimulatePublishingFailure { get; set; }

        public PartnerTransactionAcceptedV1? Message { get; private set; }

        public int CallCount { get; private set; }

        public Task PublishAsync(
            PartnerTransactionAcceptedV1 message,
            CancellationToken cancellationToken)
        {
            CallCount++;

            if (SimulatePublishingFailure)
            {
                throw new TransactionMessagePublishingException(new IOException("Broker unavailable."));
            }

            Message = message;
            return Task.CompletedTask;
        }
    }
}
