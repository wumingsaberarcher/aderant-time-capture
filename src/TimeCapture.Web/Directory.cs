namespace TimeCapture;

public sealed record Timekeeper(string Id, string Name, string OfficeTimeZone, bool PaMayRecord);

public sealed record Client(string Id, string Name);

public sealed record Matter(string Id, string ClientId, string Name);

public sealed record TimeEntry(
    string Id,
    string TimekeeperId,
    string TimekeeperName,
    string ClientId,
    string ClientName,
    string MatterId,
    string MatterName,
    decimal DurationHours,
    string ActivityCategory,
    string Comment,
    string EnteredBy,
    string Source,
    string OfficeTimeZone,
    DateTimeOffset CreatedAtUtc,
    string IdempotencyKey);

public sealed record CreateEntryRequest(
    string? TimekeeperId,
    string? ClientId,
    string? MatterId,
    decimal DurationHours,
    string? ActivityCategory,
    string? Comment,
    string? EnteredBy,
    string? Source,
    string? IdempotencyKey);

public static class FirmDirectory
{
    public static readonly Timekeeper[] Timekeepers =
    [
        new("alice", "Alice Chen", "Pacific/Auckland", true),
        new("john", "John Smith", "Europe/London", false)
    ];

    public static readonly Client[] Clients =
    [
        new("northwind", "Northwind Legal"),
        new("contoso", "Contoso Holdings")
    ];

    public static readonly Matter[] Matters =
    [
        new("M-1001", "northwind", "M-1001 Discovery"),
        new("M-1002", "northwind", "M-1002 Hearing"),
        new("M-2001", "contoso", "M-2001 Contract review")
    ];

    public static readonly string[] Categories =
    [
        "Research",
        "Court appearance",
        "Phone call",
        "Drafting",
        "Meeting"
    ];
}
