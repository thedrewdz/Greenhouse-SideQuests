using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TempTest.Infrastructure.Persistence;

string settingsPath = Path.GetFullPath(
    Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "..",
        "TempTest.Functions",
        "TempTest.Functions",
        "local.settings.json"));

if (!File.Exists(settingsPath))
{
    Console.Error.WriteLine($"Could not find local settings file: {settingsPath}");
    return 1;
}

using FileStream settingsStream = File.OpenRead(settingsPath);
LocalSettings? settings = await JsonSerializer.DeserializeAsync<LocalSettings>(
    settingsStream,
    new JsonSerializerOptions(JsonSerializerDefaults.Web));

if (string.IsNullOrWhiteSpace(settings?.Values.SqlConnectionString))
{
    Console.Error.WriteLine("SqlConnectionString is missing from local.settings.json.");
    return 1;
}

DbContextOptionsBuilder<TempTestDbContext> options = new();
options.UseSqlServer(settings.Values.SqlConnectionString);

await using TempTestDbContext dbContext = new(options.Options);
DbConnection connection = dbContext.Database.GetDbConnection();

Console.WriteLine("Opening Azure SQL connection...");
await connection.OpenAsync();

await using DbCommand command = connection.CreateCommand();
command.CommandText = """
    SELECT
        SUSER_SNAME() AS [LoginName],
        DB_NAME() AS [DatabaseName],
        (
            SELECT COUNT_BIG(*)
            FROM dbo.SensorData
        ) AS [SensorDataCount],
        (
            SELECT MAX(CreatedAtUtc)
            FROM dbo.SensorData
        ) AS [LatestCreatedAtUtc];
    """;

await using DbDataReader reader = await command.ExecuteReaderAsync();

if (await reader.ReadAsync())
{
    Console.WriteLine($"Connected as: {reader.GetString(0)}");
    Console.WriteLine($"Database: {reader.GetString(1)}");
    Console.WriteLine($"SensorData rows: {reader.GetInt64(2)}");
    Console.WriteLine($"Latest CreatedAtUtc: {reader.GetFieldValue<DateTimeOffset>(3):O}");
}

Console.WriteLine("Azure SQL connection succeeded.");
return 0;

internal sealed record LocalSettings(LocalSettingsValues Values);

internal sealed record LocalSettingsValues(string? SqlConnectionString);
