namespace UpcsgWeb.Infrastructure.Persistence;

public class Heartbeat
{
    public static readonly Guid SingletonId = new("11111111-2222-4333-8444-555555555555");

    public Guid Id { get; set; }

    public DateTime LastPingedAt { get; set; }

    public long PingCount { get; set; }
}
