namespace PartnerIntegration.Application.Abstractions.Messaging;

public class TransactionMessagePublishingException(Exception innerException)
    : Exception("Transaction message publishing failed.", innerException);
