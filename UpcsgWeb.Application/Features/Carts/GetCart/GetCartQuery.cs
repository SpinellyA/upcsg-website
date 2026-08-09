using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Carts.GetCart;

public record GetCartQuery(Guid UserId) : IQuery<CartDto>;
