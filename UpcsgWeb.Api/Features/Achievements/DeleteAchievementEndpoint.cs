using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Achievements;

namespace UpcsgWeb.Api.Features.Achievements;

public class DeleteAchievementEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/achievements/{id:guid}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await sender.Send(new DeleteAchievementCommand(Route<Guid>("id")), ct);
        await Send.NoContentAsync(ct);
    }
}
