using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UpcsgWeb.Infrastructure.Persistence;

public class UpcsgDbContextFactory : IDesignTimeDbContextFactory<UpcsgDbContext>
{
    public UpcsgDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("UPCSG_CONNECTION");

        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException(
                "UPCSG_CONNECTION is not set. Design-time EF commands need the connection "
                + "string explicitly so they cannot quietly target the wrong database. "
                + "Run scripts/ef.ps1 instead, which reads it from the API's user-secrets "
                + "(ConnectionStrings:Production) and sets it for the one command.");
        }

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

        return new UpcsgDbContext(options);
    }

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
