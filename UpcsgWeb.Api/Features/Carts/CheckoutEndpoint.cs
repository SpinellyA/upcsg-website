using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Orders.Checkout;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Carts;

public class CheckoutEndpoint(ISender sender) : Endpoint<CheckoutRequest, OrderDto>
{
    public override void Configure()
    {
        Post("/cart/checkout");
        Summary(s => s.Summary =
            "Check out the cart. A GCash order waits for the guilder to send a reference; "
            + "a cash order goes straight to the officers to be paid in person and recorded.");
    }

    public override async Task HandleAsync(CheckoutRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await Send.OkAsync(
            await sender.Send(
                new CheckoutCommand(userId.Value, req.Note, req.PaymentMethod), ct),
            ct);
    }
}
