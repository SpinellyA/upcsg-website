using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Merch;

public record GetMerchItemQuery(Guid Id) : IQuery<MerchItemDto?>;

public record ListMerchQuery : IQuery<List<MerchItemDto>>;

public record CreateMerchCommand(MerchItemDto Item) : ICommand<MerchItemDto>;

public record UpdateMerchCommand(Guid Id, MerchItemDto Item) : ICommand<MerchItemDto>;

public record DeleteMerchCommand(Guid Id) : ICommand;
