namespace UpcsgWeb.Application.Abstractions;

/// <summary>
/// The record the caller asked for does not exist.
///
/// A handler cannot call Send.NotFoundAsync — it has no idea it is being reached over
/// HTTP — so it says what happened and lets the edge choose the status code. Without
/// this, every handler would have to return a nullable and every endpoint would have to
/// remember to check it.
/// </summary>
public class NotFoundException(string what) : Exception($"{what} was not found.");

/// <summary>
/// The caller is authenticated but this record is not theirs.
///
/// Kept distinct from <see cref="NotFoundException"/> on purpose: ownership checks live
/// in the handler, next to the query that loaded the record, rather than being repeated
/// at the edge where one forgotten check leaks another guilder's order.
/// </summary>
public class ForbiddenException(string message = "You are not allowed to do that.")
    : Exception(message);
