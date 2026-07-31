using FastEndpoints;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Members;

/// <summary>
/// Backs the CMS person page. Fetching by id rather than filtering the full list means
/// opening one officer doesn't pull down the whole roster, and a deep link works on its
/// own after a reload.
/// </summary>
public class GetMemberEndpoint(IMemberRepository members)
    : EndpointWithoutRequest<MemberDto>
{
    public override void Configure()
    {
        Get("/members/{id:int}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var found = await members.GetByIdAsync(Route<int>("id"), ct);

        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(found.ToDto(), ct);
    }
}
