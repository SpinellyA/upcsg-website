using FluentValidation;

namespace UpcsgWeb.Application.Features.Opportunities;

public class CreateOpportunityValidator : AbstractValidator<CreateOpportunityCommand>
{
    public CreateOpportunityValidator()
    {
        RuleFor(c => c.Opportunity.Title)
            .NotEmpty().WithMessage("An opportunity needs a title.")
            .MaximumLength(250).WithMessage("That title is too long.");

        RuleFor(c => c.Opportunity.Url)
            .Must(OpportunityRules.BeAWebLink)
            .WithMessage("The link must be a full http or https URL.");
    }
}

public class UpdateOpportunityValidator : AbstractValidator<UpdateOpportunityCommand>
{
    public UpdateOpportunityValidator()
    {
        RuleFor(c => c.Opportunity.Title)
            .NotEmpty().WithMessage("An opportunity needs a title.")
            .MaximumLength(250).WithMessage("That title is too long.");

        RuleFor(c => c.Opportunity.Url)
            .Must(OpportunityRules.BeAWebLink)
            .WithMessage("The link must be a full http or https URL.");
    }
}

internal static class OpportunityRules
{
    internal static bool BeAWebLink(string? url) =>
        string.IsNullOrWhiteSpace(url)
        || (Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps));
}
