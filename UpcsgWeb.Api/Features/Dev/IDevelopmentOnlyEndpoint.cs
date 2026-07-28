namespace UpcsgWeb.Api.Features.Dev;

/// <summary>
/// Marks an endpoint that must never be registered outside Development.
/// Program.cs filters these out of the endpoint registry, so the route does not exist
/// in production rather than existing and refusing.
///
/// This is a marker interface rather than a namespace convention on purpose. The filter
/// used to match on the namespace string, and moving this file during a refactor
/// silently stopped it matching — which would have shipped a public endpoint capable of
/// minting admin tokens. A type reference breaks the build instead of failing quietly.
/// </summary>
public interface IDevelopmentOnlyEndpoint;
