using UpcsgWeb.Application.Abstractions;

namespace UpcsgWeb.Application.Features.Carts.ClearCart;

public record ClearCartCommand(Guid UserId) : ICommand;
