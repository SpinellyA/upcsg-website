namespace UpcsgWeb.Domain.Common;

/// <summary>
/// Thrown when an operation would break an invariant. Endpoints translate this into a
/// 409/400 rather than letting it surface as a 500 — an invalid transition is a client
/// mistake, not a server fault.
/// </summary>
public class DomainException(string message) : Exception(message);
