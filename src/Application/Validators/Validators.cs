using FluentValidation;
using TaxiCompare.Application.DTOs;

namespace TaxiCompare.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100)
            .Matches("[A-Z]").WithMessage("Password must contain uppercase letter")
            .Matches("[0-9]").WithMessage("Password must contain a digit");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).Matches(@"^\+?[0-9\s\-]{7,15}$").When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class PriceComparisonRequestValidator : AbstractValidator<PriceComparisonRequest>
{
    public PriceComparisonRequestValidator()
    {
        RuleFor(x => x.OriginAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DestinationAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.OriginLat).InclusiveBetween(-90, 90);
        RuleFor(x => x.OriginLng).InclusiveBetween(-180, 180);
        RuleFor(x => x.DestinationLat).InclusiveBetween(-90, 90);
        RuleFor(x => x.DestinationLng).InclusiveBetween(-180, 180);
    }
}
