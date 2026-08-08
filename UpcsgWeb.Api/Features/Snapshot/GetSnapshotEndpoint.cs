using FastEndpoints;
using MediatR;
using UpcsgWeb.Application.Features.Snapshot;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Snapshot;

public class GetSnapshotEndpoint(ISender sender) : EndpointWithoutRequest<ContentSnapshot>
{
    public override void Configure()
    {
        Get("/snapshot");
        AllowAnonymous();
        Summary(s => s.Summary = "Everything the public pages need, for offline fallback.");
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new GetSnapshotQuery(), ct), ct);
}
