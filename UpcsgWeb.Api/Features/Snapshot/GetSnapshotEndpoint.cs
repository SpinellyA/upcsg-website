using FastEndpoints;
using MediatR;
using UpcsgWeb.Application.Features.Snapshot;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Snapshot;

/// <summary>
/// The whole public catalogue in one document, for the offline fallback.
///
/// Anonymous, because every field in it is already served publicly one endpoint at a
/// time — aggregating public data does not make it private. That also means the GitHub
/// Action that commits the snapshot needs no credentials, which is the difference
/// between automating this safely and putting a token somewhere it can leak.
/// </summary>
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
