namespace ZemplerTicketing.Contracts;

public record EventDetailResponse(
    int Id,
    string Name,
    DateTime StartsAt,
    int TotalSeats,
    int AvailableCount,
    int ReservedCount,
    int SoldCount
);

public record TicketResponse(
    int Id,
    int EventId,
    string Status,
    string? HolderName,
    DateTime? ReservedAt
);
