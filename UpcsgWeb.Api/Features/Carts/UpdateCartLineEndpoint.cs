using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Carts.UpdateCartLine;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Carts;

public class UpdateCartLineEndpoint(ISender sender) : Endpoint<UpdateCartLineRequest, CartDto>
{
    public override void Configure()
    {
        Patch("/cart/items");
        Summary(s => s.Summary = "Set an absolute quantity; zero removes the line.");
    }

    public override async Task HandleAsync(UpdateCartLineRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var cart = await sender.Send(
            new UpdateCartLineCommand(userId.Value, req.MerchItemId, req.Variant, req.Quantity), ct);

        await Send.OkAsync(cart, ct);
    }
}
