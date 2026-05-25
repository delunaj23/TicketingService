using Microsoft.EntityFrameworkCore;
using ZemplerTicketing.Models;

namespace ZemplerTicketing.Data;

public class TicketingDbContext : DbContext
{
    public TicketingDbContext(DbContextOptions<TicketingDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasMany(e => e.Tickets)
                  .WithOne(t => t.Event)
                  .HasForeignKey(t => t.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Status)
                  .HasConversion<int>();
            entity.Property(t => t.HolderName)
                  .HasMaxLength(200);

            // Optimistic concurrency: EF Core adds "WHERE ConcurrencyToken = @original" to UPDATEs.
            // If another request changed the token first, SaveChanges throws DbUpdateConcurrencyException.
            entity.Property(t => t.ConcurrencyToken)
                  .IsConcurrencyToken();
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>().HasData(new Event
        {
            Id = 1,
            Name = "Live Coding Lounge - Friday Night",
            StartsAt = new DateTime(2026, 6, 1, 19, 0, 0, DateTimeKind.Utc),
            TotalSeats = 50
        });

        var tickets = Enumerable.Range(1, 50).Select(i => new Ticket
        {
            Id = i,
            EventId = 1,
            Status = TicketStatus.Available,
            ConcurrencyToken = Guid.NewGuid()
        }).ToArray();

        modelBuilder.Entity<Ticket>().HasData(tickets);
    }
}
