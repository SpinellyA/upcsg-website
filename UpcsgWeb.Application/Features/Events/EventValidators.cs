using FluentValidation;

namespace UpcsgWeb.Application.Features.Events;

public class ListEventsForMonthValidator : AbstractValidator<ListEventsForMonthQuery>
{
    public ListEventsForMonthValidator()
    {
        RuleFor(q => q.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("Month must be between 1 and 12.");

        RuleFor(q => q.Year)
            .InclusiveBetween(1, 9999)
            .WithMessage("That is not a real year.");
    }
}
