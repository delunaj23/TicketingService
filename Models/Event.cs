namespace ZemplerTicketing.Models;

public class Event
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public int TotalSeats { get; set; }
    public List<Ticket> Tickets { get; set; } = new();
}
