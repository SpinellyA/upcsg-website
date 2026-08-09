using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Domain.ValueObjects;
using UpcsgWeb.Shared.Contracts;
using DomainMerchItem = UpcsgWeb.Domain.Merch.MerchItem;

namespace UpcsgWeb.Application.Features.Merch;

public class GetMerchItemQueryHandler(IUnitOfWork uow)
    : IQueryHandler<GetMerchItemQuery, MerchItemDto?>
{
    public async Task<MerchItemDto?> Handle(GetMerchItemQuery query, CancellationToken ct)
    {
        var found = await uow.Merch.GetByIdAsync(query.Id, ct);
        return found?.ToDto();
    }
}

public class ListMerchQueryHandler(IUnitOfWork uow) : IQueryHandler<ListMerchQuery, List<MerchItemDto>>
{
    public async Task<List<MerchItemDto>> Handle(ListMerchQuery query, CancellationToken ct)
    {
        var items = await uow.Merch.GetAllAsync(ct);
        return [.. items.Select(m => m.ToDto())];
    }
}

public class CreateMerchCommandHandler(IUnitOfWork uow) : ICommandHandler<CreateMerchCommand, MerchItemDto>
{
    public async Task<MerchItemDto> Handle(CreateMerchCommand command, CancellationToken ct)
    {
        var dto = command.Item;

        var item = DomainMerchItem.Create(dto.Name, dto.Description, Money.Of(dto.Price));
        MerchWrites.Apply(item, dto);

        uow.Merch.Add(item);
        await uow.SaveChangesAsync(ct);

        return item.ToDto();
    }
}

public class UpdateMerchCommandHandler(IUnitOfWork uow) : ICommandHandler<UpdateMerchCommand, MerchItemDto>
{
    public async Task<MerchItemDto> Handle(UpdateMerchCommand command, CancellationToken ct)
    {
        var item = await uow.Merch.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("That merch item");

        MerchWrites.Apply(item, command.Item);
        await uow.SaveChangesAsync(ct);

        return item.ToDto();
    }
}

public class DeleteMerchCommandHandler(IUnitOfWork uow) : ICommandHandler<DeleteMerchCommand>
{
    public async Task Handle(DeleteMerchCommand command, CancellationToken ct)
    {
        var item = await uow.Merch.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("That merch item");

        uow.Merch.Remove(item);
        await uow.SaveChangesAsync(ct);
    }
}
