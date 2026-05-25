using Microsoft.EntityFrameworkCore;
using ZemplerTicketing.Contracts;
using ZemplerTicketing.Data;
using ZemplerTicketing.Models;

namespace ZemplerTicketing.Services;

public class TicketingService : ITicketingService
{
    private readonly TicketingDbContext _db;
    private readonly ILogger<TicketingService> _logger;
    private static readonly TimeSpan ReservationTimeout = TimeSpan.FromMinutes(10);

    public TicketingService(TicketingDbContext db, ILogger<TicketingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<EventDetailResponse?> GetEventDetailAsync(int eventId)
    {
        var ev = await _db.Events
            .AsNoTracking()
            .Include(e => e.Tickets)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev is null)
        {
            _logger.LogWarning("Event {EventId} not found", eventId);
            return null;
        }

        var now = DateTime.UtcNow;

        var available = ev.Tickets.Count(t =>
            t.Status == TicketStatus.Available || t.IsReservationExpired(now));
        var reserved = ev.Tickets.Count(t =>
            t.Status == TicketStatus.Reserved && !t.IsReservationExpired(now));
        var sold = ev.Tickets.Count(t => t.Status == TicketStatus.Sold);

        _logger.LogInformation(
            "Retrieved event {EventId}: {Available} available, {Reserved} reserved, {Sold} sold",
            eventId, available, reserved, sold);

        return new EventDetailResponse(ev.Id, ev.Name, ev.StartsAt, ev.TotalSeats, available, reserved, sold);
    }

    public async Task<ServiceResult<TicketResponse>> ReserveTicketAsync(int eventId, string holderName)
    {
        var eventExists = await _db.Events.AnyAsync(e => e.Id == eventId);
        if (!eventExists)
        {
            _logger.LogWarning("Reserve failed: event {EventId} not found", eventId);
            return ServiceResult<TicketResponse>.Fail(404, $"Event {eventId} not found.");
        }

        var now = DateTime.UtcNow;

        // Find a ticket that is either Available, or Reserved but expired (10-minute rule).
        // FirstOrDefault is deliberate: we grab one ticket, not all of them.
        var ticket = await _db.Tickets
            .Where(t => t.EventId == eventId)
            .Where(t =>
                t.Status == TicketStatus.Available
                || (t.Status == TicketStatus.Reserved
                    && t.ReservedAt != null
                    && t.ReservedAt.Value.AddMinutes(10) <= now))
            .FirstOrDefaultAsync();

        if (ticket is null)
        {
            _logger.LogWarning("Reserve failed: no available tickets for event {EventId}", eventId);
            return ServiceResult<TicketResponse>.Fail(409, "No available tickets for this event.");
        }

        ticket.Status = TicketStatus.Reserved;
        ticket.HolderName = holderName;
        ticket.ReservedAt = now;
        ticket.ConcurrencyToken = Guid.NewGuid();

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request grabbed this ticket between our read and write.
            // This is the core concurrency guarantee: exactly one caller wins.
            _logger.LogWarning(
                "Concurrency conflict reserving ticket {TicketId} for event {EventId} — another customer got it first",
                ticket.Id, eventId);
            return ServiceResult<TicketResponse>.Fail(409, "Another customer reserved the last ticket. Please try again.");
        }

        _logger.LogInformation(
            "Ticket {TicketId} reserved for '{HolderName}' on event {EventId}",
            ticket.Id, holderName, eventId);

        return ServiceResult<TicketResponse>.Success(ToResponse(ticket), 201);
    }

    public async Task<ServiceResult<TicketResponse>> PurchaseTicketAsync(int ticketId, string holderName)
    {
        var ticket = await _db.Tickets.FindAsync(ticketId);

        if (ticket is null)
        {
            _logger.LogWarning("Purchase failed: ticket {TicketId} not found", ticketId);
            return ServiceResult<TicketResponse>.Fail(404, $"Ticket {ticketId} not found.");
        }

        // Check if this reservation has expired before allowing purchase
        if (ticket.IsReservationExpired(DateTime.UtcNow))
        {
            ticket.Status = TicketStatus.Available;
            ticket.HolderName = null;
            ticket.ReservedAt = null;
            ticket.ConcurrencyToken = Guid.NewGuid();
            await _db.SaveChangesAsync();

            _logger.LogWarning("Purchase failed: reservation on ticket {TicketId} has expired", ticketId);
            return ServiceResult<TicketResponse>.Fail(409, "Reservation has expired. The ticket is now available again.");
        }

        if (ticket.Status != TicketStatus.Reserved)
        {
            _logger.LogWarning("Purchase failed: ticket {TicketId} is {Status}, not Reserved", ticketId, ticket.Status);
            return ServiceResult<TicketResponse>.Fail(409, $"Ticket is {ticket.Status}, not Reserved.");
        }

        if (!string.Equals(ticket.HolderName, holderName, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Purchase failed: ticket {TicketId} is reserved by '{ReservedBy}', not '{RequestedBy}'",
                ticketId, ticket.HolderName, holderName);
            return ServiceResult<TicketResponse>.Fail(409, "Ticket is not reserved by you.");
        }

        ticket.Status = TicketStatus.Sold;
        ticket.ConcurrencyToken = Guid.NewGuid();

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict purchasing ticket {TicketId}", ticketId);
            return ServiceResult<TicketResponse>.Fail(409, "Ticket state changed during your request. Please try again.");
        }

        _logger.LogInformation("Ticket {TicketId} sold to '{HolderName}'", ticketId, holderName);
        return ServiceResult<TicketResponse>.Success(ToResponse(ticket));
    }

    private static TicketResponse ToResponse(Ticket ticket) =>
        new(ticket.Id, ticket.EventId, ticket.Status.ToString(), ticket.HolderName, ticket.ReservedAt);
}
