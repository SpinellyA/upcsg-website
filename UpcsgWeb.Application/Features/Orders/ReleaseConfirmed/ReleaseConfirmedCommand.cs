using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.ReleaseConfirmed;

/// <summary>
/// Releases every confirmed order in one go, for the moment at a merch handover when a
/// queue of guilders collects at once and marking them off one at a time is the slowest
/// part of the table.
///
/// One request and one transaction rather than one call per order: a browser loop that
/// dies halfway leaves the officer unable to say which half went through.
/// </summary>
public record ReleaseConfirmedCommand : ICommand<ReleaseConfirmedDto>;
