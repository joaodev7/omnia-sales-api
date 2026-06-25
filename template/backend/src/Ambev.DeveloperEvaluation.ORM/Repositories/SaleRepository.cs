using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

/// <summary>
/// EF Core implementation of the Sale repository.
/// </summary>
public class SaleRepository : ISaleRepository
{
    private readonly DefaultContext _context;

    public SaleRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Sale?> GetBySaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.SaleNumber == saleNumber, cancellationToken);
    }

    public async Task<(List<Sale> Items, int TotalCount)> ListAsync(
        int page,
        int size,
        string? order,
        Guid? customerId = null,
        Guid? branchId = null,
        DateTime? minDate = null,
        DateTime? maxDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Sales
            .Include(s => s.Items)
            .AsNoTracking();

        // Apply filters
        if (customerId.HasValue && customerId.Value != Guid.Empty)
        {
            query = query.Where(s => s.CustomerId == customerId.Value);
        }

        if (branchId.HasValue && branchId.Value != Guid.Empty)
        {
            query = query.Where(s => s.BranchId == branchId.Value);
        }

        if (minDate.HasValue)
        {
            query = query.Where(s => s.SaleDate >= DateTime.SpecifyKind(minDate.Value, DateTimeKind.Utc));
        }

        if (maxDate.HasValue)
        {
            query = query.Where(s => s.SaleDate <= DateTime.SpecifyKind(maxDate.Value, DateTimeKind.Utc));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(order))
        {
            var parts = order.Split(',');
            IOrderedQueryable<Sale>? orderedQuery = null;
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                var isDesc = trimmed.EndsWith("desc", StringComparison.OrdinalIgnoreCase);
                var propertyName = trimmed.Split(' ')[0].Trim();

                if (propertyName.Equals("SaleNumber", StringComparison.OrdinalIgnoreCase))
                {
                    orderedQuery = orderedQuery == null
                        ? (isDesc ? query.OrderByDescending(s => s.SaleNumber) : query.OrderBy(s => s.SaleNumber))
                        : (isDesc ? orderedQuery.ThenByDescending(s => s.SaleNumber) : orderedQuery.ThenBy(s => s.SaleNumber));
                }
                else if (propertyName.Equals("SaleDate", StringComparison.OrdinalIgnoreCase))
                {
                    orderedQuery = orderedQuery == null
                        ? (isDesc ? query.OrderByDescending(s => s.SaleDate) : query.OrderBy(s => s.SaleDate))
                        : (isDesc ? orderedQuery.ThenByDescending(s => s.SaleDate) : orderedQuery.ThenBy(s => s.SaleDate));
                }
                else if (propertyName.Equals("TotalAmount", StringComparison.OrdinalIgnoreCase))
                {
                    orderedQuery = orderedQuery == null
                        ? (isDesc ? query.OrderByDescending(s => s.TotalAmount) : query.OrderBy(s => s.TotalAmount))
                        : (isDesc ? orderedQuery.ThenByDescending(s => s.TotalAmount) : orderedQuery.ThenBy(s => s.TotalAmount));
                }
            }

            if (orderedQuery != null)
            {
                query = orderedQuery;
            }
        }
        else
        {
            query = query.OrderByDescending(s => s.SaleDate);
        }

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        _context.Sales.Update(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await GetByIdAsync(id, cancellationToken);
        if (sale == null)
            return false;

        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
