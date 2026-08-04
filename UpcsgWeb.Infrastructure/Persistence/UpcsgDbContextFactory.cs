using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UpcsgWeb.Infrastructure.Persistence;

/// <summary>
/// Lets the EF tools build the model without starting the API.
///
/// Scaffolding a migration only needs the provider, not a reachable server. Applying one
/// needs the real thing, which comes from UPCSG_CONNECTION so the credentials stay out of
/// the repository.
///
/// Use scripts/ef.ps1 rather than setting that variable by hand. Pasting a connection
/// string into PowerShell truncates it at the first ';' if unquoted, and at a '#' in the
/// password even when quoted with double quotes — producing a "Format of the
/// initialization string does not conform to specification" error that points at a
/// perfectly valid string.
/// </summary>
public class UpcsgDbContextFactory : IDesignTimeDbContextFactory<UpcsgDbContext>
{
    public UpcsgDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("UPCSG_CONNECTION");

        if (string.IsNullOrWhiteSpace(connection))
        {
            // EF prefers this factory over the API's own configuration, so a silent
            // localhost fallback here would point `database drop` at the wrong server
            // while reporting success. Refuse instead.
            throw new InvalidOperationException(
                "UPCSG_CONNECTION is not set. Design-time EF commands need the connection "
                + "string explicitly so they cannot quietly target the wrong database. "
                + "Run scripts/ef.ps1 instead, which reads it from the API's user-secrets "
                + "(ConnectionStrings:Production) and sets it for the one command.");
        }

        // Parse it here rather than letting Npgsql fail later.
        //
        // Npgsql reports a malformed string as "Format of the initialization string does
        // not conform to specification starting at index N", where N is a character
        // position in a string nobody can see — and the position it names is usually the
        // *next* key after the real damage, so it points at something perfectly valid.
        // Nearly every case is a shell mangling the value on the way in.
        try
        {
            _ = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "UPCSG_CONNECTION is set but is not a valid connection string: "
                + ex.Message
                + $"{Environment.NewLine}{Environment.NewLine}"
                + $"It is {connection.Length} characters long and starts: "
                + $"'{Preview(connection)}'."
                + $"{Environment.NewLine}{Environment.NewLine}"
                + "This is almost always the shell, not the string. A value pasted into "
                + "PowerShell unquoted ends at the first ';', and a '#' in the password "
                + "starts a comment; cmd's `set VAR=\"...\"` keeps the quotes as part of the "
                + "value. Use scripts/ef.ps1, which reads the string from user-secrets and "
                + "never passes it through a command line.",
                ex);
        }

        var options = new DbContextOptionsBuilder<UpcsgDbContext>()
            .UseNpgsql(connection)
            .Options;

        // No dispatcher: design time never saves, so nothing can be published.
        return new UpcsgDbContext(options);
    }

    /// <summary>
    /// Enough of the string to recognise which one it is, stopping before the password.
    /// Truncated at the third ';' because Host, Port and Database identify the target
    /// while Username and Password do not need to appear in a console error.
    /// </summary>
    private static string Preview(string connection)
    {
        var cut = 0;

        for (var found = 0; found < 3 && cut >= 0; found++)
        {
            cut = connection.IndexOf(';', cut + 1);
        }

        return cut > 0 ? connection[..cut] : connection[..Math.Min(40, connection.Length)];
    }
}
