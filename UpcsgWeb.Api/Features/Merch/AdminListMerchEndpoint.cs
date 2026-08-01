using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Merch;

/// <summary>
/// The CMS grid. Identical to the public list today, but kept separate so the public
/// view can start filtering (drafts, scheduled drops) without changing the admin one.
/// </summary>
public class AdminListMerchEndpoint(IMerchRepository merch) : EndpointWithoutRequest<List<MerchItemDto>>
{
    public override void Configure()
    {
        Get("/admin/merch");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var items = await merch.GetAllAsync(ct);
        await Send.OkAsync([.. items.Select(m => m.ToDto())], ct);
    }
}
