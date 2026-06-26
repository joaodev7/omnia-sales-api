using Ambev.DeveloperEvaluation.Application.Common.Events;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleHandler : IRequestHandler<CreateSaleCommand, CreateSaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IEventPublisher _eventPublisher;

    public CreateSaleHandler(
        ISaleRepository saleRepository,
        IMapper mapper,
        IEventPublisher eventPublisher)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
    }

    public async Task<CreateSaleResult> Handle(CreateSaleCommand command, CancellationToken cancellationToken)
    {
        // 1. Generate Sale Number
        // e.g. SALE-YYYYMMDD-RandomHex
        var saleNumber = $"SALE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        // 3. Create domain entity
        var sale = new Sale(
            saleNumber,
            command.CustomerId,
            command.CustomerName,
            command.BranchId,
            command.BranchName
        );

        // 4. Add items using domain logic (so it triggers validation & discount rules)
        foreach (var item in command.Items)
        {
            sale.AddItem(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice);
        }

        // 5. Persist
        var createdSale = await _saleRepository.CreateAsync(sale, cancellationToken);

        // 6. Publish Event
        await _eventPublisher.PublishAsync(new SaleCreatedEvent(createdSale), cancellationToken);

        // 7. Return mapped result
        return _mapper.Map<CreateSaleResult>(createdSale);
    }
}
