using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Carts.AddToCart;

public record AddToCartCommand(Guid UserId, Guid MerchItemId, string? Variant, int Quantity)
    : ICommand<CartDto>;
