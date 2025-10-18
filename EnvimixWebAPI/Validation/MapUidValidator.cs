using FluentValidation;

namespace EnvimixWebAPI.Validation;

internal sealed class MapUidValidator : AbstractValidator<string>
{
    public MapUidValidator()
    {
        RuleFor(x => x)
            .NotEmpty()
            .WithMessage("MapUid is required")
            .MinimumLength(24)
            .WithMessage("MapUid is too short")
            .MaximumLength(34)
            .WithMessage("MapUid is too long")
            .Matches(RegexUtils.MapUidRegex())
            .WithMessage("MapUid is invalid");
    }
}
