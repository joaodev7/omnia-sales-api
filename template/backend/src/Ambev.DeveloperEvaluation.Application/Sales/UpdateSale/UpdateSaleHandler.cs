using Ambev.DeveloperEvaluation.Application.Common.Events;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

public class UpdateSaleHandler : IRequestHandler<UpdateSaleCommand, UpdateSaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IEventPublisher _eventPublisher;

    public UpdateSaleHandler(
        ISaleRepository saleRepository,
        IMapper mapper,
        IEventPublisher eventPublisher)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
    }

    public async Task<UpdateSaleResult> Handle(UpdateSaleCommand command, CancellationToken cancellationToken)
    {
        // 1. Validate request
        var validator = new UpdateSaleValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // 2. Retrieve existing Sale
        var sale = await _saleRepository.GetByIdAsync(command.Id, cancellationToken);
        if (sale == null)
            throw new KeyNotFoundException($"Sale with ID {command.Id} not found");

        // 3. Update properties
        sale.CustomerId = command.CustomerId;
        sale.CustomerName = command.CustomerName;
        sale.BranchId = command.BranchId;
        sale.BranchName = command.BranchName;

        // 4. Reconcile items
        // Find items to remove (exist in DB but not in Command)
        var itemsToRemove = sale.Items
            .Where(dbItem => !command.Items.Any(cmdItem => cmdItem.ProductId == dbItem.ProductId))
            .ToList();

        foreach (var item in itemsToRemove)
        {
            sale.RemoveItem(item.Id);
        }

        // Add or update items
        var cancelledItems = new List<SaleItem>();
        foreach (var cmdItem in command.Items)
        {
            var existingItem = sale.Items.FirstOrDefault(i => i.ProductId == cmdItem.ProductId);
            if (existingItem != null)
            {
                if (existingItem.IsCancelled)
                {
                    // Item cancelado não deve ser reativado através de uma atualização comum.
                    if (!cmdItem.IsCancelled)
                        throw new DomainException($"Cannot reactivate a cancelled item for product ID {cmdItem.ProductId}.");
                }
                else
                {
                    if (cmdItem.IsCancelled)
                    {
                        sale.CancelItem(existingItem.Id);
                        cancelledItems.Add(existingItem);
                    }
                    else
                    {
                        existingItem.UpdatePriceAndQuantity(cmdItem.UnitPrice, cmdItem.Quantity);
                    }
                }
            }
            else
            {
                if (cmdItem.IsCancelled)
                {
                    sale.AddItem(cmdItem.ProductId, cmdItem.ProductName, cmdItem.Quantity, cmdItem.UnitPrice);
                    var added = sale.Items.First(i => i.ProductId == cmdItem.ProductId);
                    sale.CancelItem(added.Id);
                    cancelledItems.Add(added);
                }
                else
                {
                    sale.AddItem(cmdItem.ProductId, cmdItem.ProductName, cmdItem.Quantity, cmdItem.UnitPrice);
                }
            }
        }

        sale.RecalculateTotals();

        // 5. Save changes
        var updatedSale = await _saleRepository.UpdateAsync(sale, cancellationToken);

        // 6. Publish Event
        await _eventPublisher.PublishAsync(new SaleModifiedEvent(updatedSale), cancellationToken);
        foreach (var cancelledItem in cancelledItems)
        {
            await _eventPublisher.PublishAsync(new ItemCancelledEvent(cancelledItem), cancellationToken);
        }

        // 7. Return response
        return _mapper.Map<UpdateSaleResult>(updatedSale);
    }
}
