# ZemplerTicketing

Small ASP.NET Core Web API for an event-ticketing system. An Event has many
Tickets; tickets move through `Available → Reserved → Sold`. Reservations
expire after 10 minutes.

## Stack

- .NET 10 / ASP.NET Core 10
- EF Core 10 (SQLite)
- Swashbuckle (Swagger UI)

## Run

```bash
dotnet run
```

First run creates `ticketing.db` and seeds one event with 50 available
tickets. Swagger UI is at `/swagger` on the URL printed at startup.

See `ZemplerTicketing.http` for sample requests.

## Endpoints

| Method | Route                          | Purpose                                                       |
|--------|--------------------------------|---------------------------------------------------------------|
| GET    | `/api/events/{id}`             | Event details with available / reserved / sold counts.        |
| POST   | `/api/events/{id}/reserve`     | Reserve one available ticket. Body: `{ "holderName": "..." }`.|
| POST   | `/api/tickets/{id}/purchase`   | Mark a reserved ticket as Sold. 409 if not held by caller.    |
