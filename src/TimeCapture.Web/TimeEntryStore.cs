namespace TimeCapture;

public sealed class TimeEntryStore
{
    private readonly object _gate = new();
    private readonly List<TimeEntry> _entries = [];
    private readonly Dictionary<string, TimeEntry> _byIdempotency = new(StringComparer.Ordinal);

    public IReadOnlyList<TimeEntry> All()
    {
        lock (_gate)
            return _entries.ToArray();
    }

    public (int Status, object Body) Create(CreateEntryRequest request, bool simulateLatency)
    {
        if (simulateLatency)
            Thread.Sleep(600);

        var source = string.IsNullOrWhiteSpace(request.Source) ? "Ui" : request.Source.Trim();
        var enteredBy = string.IsNullOrWhiteSpace(request.EnteredBy) ? "Lawyer" : request.EnteredBy.Trim();
        var comment = (request.Comment ?? "").Trim();
        var category = (request.ActivityCategory ?? "").Trim();
        var idempotency = (request.IdempotencyKey ?? "").Trim();

        if (string.IsNullOrEmpty(idempotency))
            return Error(400, "Idempotency key is required so a delayed double-save cannot create two invoices.");

        lock (_gate)
        {
            if (_byIdempotency.TryGetValue(idempotency, out var existing))
                return (200, existing);

            var timekeeper = FirmDirectory.Timekeepers.FirstOrDefault(t => t.Id == request.TimekeeperId);
            if (timekeeper is null)
            {
                if (source.Equals("Integration", StringComparison.OrdinalIgnoreCase))
                    return Error(400, "Unknown timekeeper. Integration writes are rejected so a bad id cannot bill a client.");
                return Error(400, "Select who the lawyer is.");
            }

            if (enteredBy.Equals("PA", StringComparison.OrdinalIgnoreCase) && !timekeeper.PaMayRecord)
                return Error(403, "This PA is not allowed to record time for that lawyer.");

            var client = FirmDirectory.Clients.FirstOrDefault(c => c.Id == request.ClientId);
            if (client is null)
                return Error(400, "Select a client.");

            var matter = FirmDirectory.Matters.FirstOrDefault(m => m.Id == request.MatterId);
            if (matter is null)
                return Error(400, "Select a matter. The wrong or missing matter bills the wrong client.");

            if (matter.ClientId != client.Id)
                return Error(400, "That matter does not belong to the selected client.");

            if (request.DurationHours <= 0)
                return Error(400, "Duration must be greater than zero.");

            if (request.DurationHours * 10 % 1 != 0)
                return Error(400, "Duration must be in 0.1 hour (6 minute) units.");

            if (string.IsNullOrEmpty(category) || !FirmDirectory.Categories.Contains(category))
                return Error(400, "Select a time-activity category.");

            if (string.IsNullOrEmpty(comment))
                return Error(400, "A comment is required so finance can invoice the work.");

            var entry = new TimeEntry(
                Id: Guid.NewGuid().ToString("n")[..8],
                TimekeeperId: timekeeper.Id,
                TimekeeperName: timekeeper.Name,
                ClientId: client.Id,
                ClientName: client.Name,
                MatterId: matter.Id,
                MatterName: matter.Name,
                DurationHours: request.DurationHours,
                ActivityCategory: category,
                Comment: comment,
                EnteredBy: enteredBy,
                Source: source,
                OfficeTimeZone: timekeeper.OfficeTimeZone,
                CreatedAtUtc: DateTimeOffset.UtcNow,
                IdempotencyKey: idempotency);

            _entries.Insert(0, entry);
            _byIdempotency[idempotency] = entry;
            return (200, entry);
        }
    }

    private static (int Status, object Body) Error(int status, string message) =>
        (status, new { error = message });
}
