using ZemplerTicketing.Contracts;

namespace ZemplerTicketing.Services;

public interface ITicketingService
{
    Task<EventDetailResponse?> GetEventDetailAsync(int eventId);
    Task<ServiceResult<TicketResponse>> ReserveTicketAsync(int eventId, string holderName);
    Task<ServiceResult<TicketResponse>> PurchaseTicketAsync(int ticketId, string holderName);
}

public record ServiceResult<T>
{
    public T? Value { get; init; }
    public int StatusCode { get; init; }
    public string? Error { get; init; }

    public static ServiceResult<T> Success(T value, int statusCode = 200) =>
        new() { Value = value, StatusCode = statusCode };

    public static ServiceResult<T> Fail(int statusCode, string error) =>
        new() { StatusCode = statusCode, Error = error };
}
