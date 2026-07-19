using PartnerIntegration.Application.Abstractions.ExternalServices;
using PartnerIntegration.Application.Abstractions.Messaging;
using PartnerIntegration.Application.Common.Results;
using PartnerIntegration.Application.Transactions.CreateTransaction;

namespace PartnerIntegration.UnitTests;

public class CreateTransactionCommandHandlerTests
{
    private static readonly DateTimeOffset AcceptedAt = new(2026, 7, 17, 8, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Handle_WhenPartnerIsVerified_ShouldNormalizeAndPublishTransactionMessage()
    {
        var verificationService = new StubPartnerVerificationService(isVerified: true);
        var messagePublisher = new CapturingMessagePublisher();
        var commandHandler = CreateHandler(verificationService, messagePublisher);
        using var cancellation = new CancellationTokenSource();
        var command = CreateValidCommand() with
        {
            PartnerId = " P-1001 ",
            TransactionReference = " TXN-99823 ",
            Currency = "usd"
        };

        var result = await commandHandler.Handle(command, cancellation.Token);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(messagePublisher.Message, Is.Not.Null);
        var message = messagePublisher.Message!;
        Assert.That(message.PartnerId, Is.EqualTo("P-1001"));
        Assert.That(message.TransactionReference, Is.EqualTo("TXN-99823"));
        Assert.That(message.Currency, Is.EqualTo("USD"));
        Assert.That(message.CorrelationId, Is.EqualTo("correlation-001"));
        Assert.That(message.AcceptedAtUtc, Is.EqualTo(AcceptedAt));
        Assert.That(messagePublisher.CancellationToken, Is.EqualTo(cancellation.Token));
    }

    [Test]
    public async Task Handle_WhenPartnerIsNotVerified_ShouldReturnBusinessFailureWithoutPublishing()
    {
        var verificationService = new StubPartnerVerificationService(isVerified: false);
        var messagePublisher = new CapturingMessagePublisher();
        var commandHandler = CreateHandler(verificationService, messagePublisher);

        var result = await commandHandler.Handle(CreateValidCommand(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Category, Is.EqualTo(ErrorCategory.BusinessRule));
        Assert.That(messagePublisher.Message, Is.Null);
    }

    [Test]
    public async Task Handle_WhenPartnerIdentityDoesNotMatch_ShouldReturnAccessDeniedWithoutCallingDependencies()
    {
        var verificationService = new StubPartnerVerificationService(isVerified: true);
        var messagePublisher = new CapturingMessagePublisher();
        var commandHandler = CreateHandler(verificationService, messagePublisher);
        var command = CreateValidCommand() with { AuthenticatedPartnerId = "P-OTHER" };

        var result = await commandHandler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Category, Is.EqualTo(ErrorCategory.AccessDenied));
        Assert.That(verificationService.CallCount, Is.Zero);
        Assert.That(messagePublisher.Message, Is.Null);
    }

    private static CreateTransactionCommandHandler CreateHandler(
        IPartnerVerificationService verificationService,
        ITransactionMessagePublisher messagePublisher) =>
        new(verificationService, messagePublisher, new FixedTimeProvider());

    private static CreateTransactionCommand CreateValidCommand() =>
        new(
            "P-1001",
            "correlation-001",
            "P-1001",
            "TXN-99823",
            250m,
            "USD",
            AcceptedAt.AddDays(-1));

    private class StubPartnerVerificationService(bool isVerified) : IPartnerVerificationService
    {
        public int CallCount { get; private set; }

        public Task<bool> VerifyAsync(string partnerId, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(isVerified);
        }
    }

    private class CapturingMessagePublisher : ITransactionMessagePublisher
    {
        public PartnerTransactionAcceptedV1? Message { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task PublishAsync(PartnerTransactionAcceptedV1 message, CancellationToken cancellationToken)
        {
            Message = message;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => AcceptedAt;
    }
}
