using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.ReleaseConfirmed;

public record ReleaseConfirmedCommand : ICommand<ReleaseConfirmedDto>;
