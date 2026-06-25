using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesValidator : AbstractValidator<ListSalesQuery>
{
    public ListSalesValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page number must be greater than zero.");

        RuleFor(x => x.Size)
            .GreaterThan(0).WithMessage("Page size must be greater than zero.")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100.");
    }
}
