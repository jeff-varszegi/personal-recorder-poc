using DbUp;
using DbUp.Engine;

namespace PersonalRecorder.Service.Data;

/// <summary>
/// Applies the schema migrations embedded from <c>Data/Migrations/*.sql</c>, in file-name
/// order, each in its own transaction. Plain SQL rather than generated code, so the schema is
/// reviewed as SQL (ADR-015). Safe to run repeatedly: applied scripts are journaled in
/// <c>schemaversions</c>.
/// </summary>
public static class DatabaseMigrator
{
    /// <summary>Creates the database if needed and applies pending scripts. Returns <see langword="true"/> on success.</summary>
    public static bool Upgrade(string connectionString)
    {
        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        UpgradeEngine upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                typeof(DatabaseMigrator).Assembly,
                name => name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        DatabaseUpgradeResult result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            Console.Error.WriteLine(result.Error);
        }

        return result.Successful;
    }
}
