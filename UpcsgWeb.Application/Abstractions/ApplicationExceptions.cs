namespace UpcsgWeb.Application.Abstractions;

public class NotFoundException(string what) : Exception($"{what} was not found.");

public class ForbiddenException(string message = "You are not allowed to do that.")
    : Exception(message);

public class UnauthorizedException(string message = "You are not signed in.")
    : Exception(message);
