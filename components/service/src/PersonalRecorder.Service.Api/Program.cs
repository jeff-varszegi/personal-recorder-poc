// Personal Recorder — service host.
//
// Scaffold only. Endpoints, persistence, SignalR hubs, and the worker registrations
// (Ingest, Artifacts, Circles, Alerts, Heartbeat, Sharing — docs/ARCHITECTURE.md §9.1)
// land once the domain model is designed. The service is deliberately a single process
// at MVP: PostgreSQL + S3-compatible storage + this host (ADR-005).

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .WithName("Health")
   .WithSummary("Liveness probe.");

app.Run();
