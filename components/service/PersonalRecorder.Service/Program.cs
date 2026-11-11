// Personal Recorder — service host.
//
// One process is the whole back end at MVP (ADR-005): controllers, SignalR hubs and hosted
// workers in here; PostgreSQL and S3-compatible object storage beside it. Feature code
// (ARCHITECTURE.md §9.1) lands once the domain model is designed; this file wires the
// conventions the features build on (ADR-015): controllers, Dapper over Npgsql, DbUp.
//
//   dotnet run                 start the host
//   dotnet run -- migrate      apply pending SQL migrations and exit

using Npgsql;
using PersonalRecorder.Service.Data;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Database is not configured. Development reads it from " +
        "appsettings.Development.json; other environments supply ConnectionStrings__Database.");

if (args is ["migrate"])
{
    return DatabaseMigrator.Upgrade(connectionString) ? 0 : 1;
}

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// One pooled data source for the process. Nothing connects until a feature asks for a
// connection, so the host starts without a database and fails per request, visibly.
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
return 0;
