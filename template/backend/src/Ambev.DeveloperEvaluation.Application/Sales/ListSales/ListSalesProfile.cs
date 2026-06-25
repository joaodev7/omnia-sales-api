using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesProfile : Profile
{
    public ListSalesProfile()
    {
        // Re-use mappings or define if needed
        CreateMap<Sale, GetSaleResult>();
        CreateMap<SaleItem, GetSaleItemResult>();
    }
}
