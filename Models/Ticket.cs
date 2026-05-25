namespace ZemplerTicketing.Models;

public class Ticket
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string? HolderName { get; set; }
    public TicketStatus Status { get; set; }
    public DateTime? ReservedAt { get; set; }

    // EF Core uses this in the WHERE clause of UPDATEs to detect concurrent modifications.
    // SQLite doesn't support SQL Server's rowversion, so we use a Guid that changes on every write.
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();

    public bool IsReservationExpired(DateTime utcNow)
    {
        return Status == TicketStatus.Reserved
            && ReservedAt.HasValue
            && ReservedAt.Value.AddMinutes(10) <= utcNow;
    }
}
