using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Orders.Checkout;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Carts;

/// <summary>
/// Turns the guilder's cart into an order awaiting payment.
///
/// Both the new order and the emptied cart are written by a single SaveChangesAsync —
/// that's the unit of work earning its keep. Committing them separately could leave an
/// order placed with the cart still full, and a refresh would order everything twice.
/// </summary>
public class CheckoutEndpoint(ISender sender) : Endpoint<CheckoutRequest, OrderDto>
{
    public override void Configure()
    {
        Post("/cart/checkout");
        Summary(s => s.Summary = "Check out the cart. Creates an order awaiting a GCash receipt.");
    }

    public override async Task HandleAsync(CheckoutRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await Send.OkAsync(await sender.Send(new CheckoutCommand(userId.Value, req.Note), ct), ct);
    }
}
