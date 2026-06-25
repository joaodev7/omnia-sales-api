using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesQuery : IRequest<ListSalesResult>
{
    public int Page { get; }
    public int Size { get; }
    public string? Order { get; }
    public Guid? CustomerId { get; }
    public Guid? BranchId { get; }
    public DateTime? MinDate { get; }
    public DateTime? MaxDate { get; }

    public ListSalesQuery(
        int page,
        int size,
        string? order,
        Guid? customerId = null,
        Guid? branchId = null,
        DateTime? minDate = null,
        DateTime? maxDate = null)
    {
        Page = page;
        Size = size;
        Order = order;
        CustomerId = customerId;
        BranchId = branchId;
        MinDate = minDate;
        MaxDate = maxDate;
    }
}
