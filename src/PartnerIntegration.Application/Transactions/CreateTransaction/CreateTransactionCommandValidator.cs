using FluentValidation;
using PartnerIntegration.Application.Abstractions.ReferenceData;
using PartnerIntegration.Domain.Transactions;

namespace PartnerIntegration.Application.Transactions.CreateTransaction;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator(ISupportedCurrencyProvider supportedCurrencies)
    {
        RuleFor(command => command.AuthenticatedPartnerId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Authenticated partner ID is required.")
            .MaximumLength(PartnerId.MaxLength);

        RuleFor(command => command.PartnerId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Partner ID is required.")
            .MaximumLength(PartnerId.MaxLength);

        RuleFor(command => command.TransactionReference)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Transaction reference is required.")
            .MaximumLength(TransactionReference.MaxLength);

        RuleFor(command => command.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(command => command.Currency)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Length(Money.CurrencyCodeLength)
            .WithMessage("Currency must be a three-letter code.")
            .Must(supportedCurrencies.IsSupported)
            .WithMessage("Currency is not supported.");

        RuleFor(command => command.Timestamp)
            .NotEqual(default(DateTimeOffset))
            .WithMessage("Timestamp is required.");
    }
}
