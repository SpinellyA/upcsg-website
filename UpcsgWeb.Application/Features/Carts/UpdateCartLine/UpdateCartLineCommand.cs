using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Carts.UpdateCartLine;

public record UpdateCartLineCommand(Guid UserId, Guid MerchItemId, string? Variant, int Quantity)
    : ICommand<CartDto>;
