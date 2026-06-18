using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace PlaylistRepoAPI;

public static class LegacyDatabaseUpgrader
{
	public static async Task UpgradeIfNeededAsync(IServiceProvider services)
	{
		using var scope = services.CreateScope();
		var repo = scope.ServiceProvider.GetRequiredService<IPlayRepoService>();

		if (!repo.IsRepoInitialized || repo.DotDir is null)
			return;

		string dbPath = Path.Combine(repo.DotDir!.FullName, "library.db");

		using var dbScope = services.CreateScope();
		var db = dbScope.ServiceProvider.GetRequiredService<PlayRepoDbContext>();

		if (!File.Exists(dbPath))
		{
			await db.Database.MigrateAsync();
			return;
		}

		bool hasMigrationHistory = await TableExistsAsync(dbPath, "__EFMigrationsHistory");
		if (hasMigrationHistory)
		{
			await db.Database.MigrateAsync();
			return;
		}

		await RebuildLegacyDatabaseAsync(services, dbPath);
	}

	private static async Task<bool> TableExistsAsync(string dbPath, string tableName)
	{
		await using var connection = new SqliteConnection($"Data Source={dbPath}");
		await connection.OpenAsync();

		await using var command = connection.CreateCommand();
		command.CommandText = """
			SELECT 1
			FROM sqlite_master
			WHERE type = 'table' AND name = $tableName
			LIMIT 1;
			""";
		command.Parameters.AddWithValue("$tableName", tableName);

		var result = await command.ExecuteScalarAsync();
		return result is not null;
	}

	private static async Task RebuildLegacyDatabaseAsync(IServiceProvider services, string dbPath)
	{
		string backupPath = dbPath + ".legacy.bak";
		if (File.Exists(backupPath))
			File.Delete(backupPath);

		SqliteConnection.ClearAllPools();
		File.Move(dbPath, backupPath, overwrite: true);

		using (var scope = services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<PlayRepoDbContext>();
			await db.Database.MigrateAsync();
		}

		await using var connection = new SqliteConnection($"Data Source={dbPath}");
		await connection.OpenAsync();

		await using var command = connection.CreateCommand();
		command.CommandText = """
			PRAGMA foreign_keys = OFF;

			ATTACH DATABASE $legacyPath AS legacy;

			INSERT INTO "RemotePlaylists"
			SELECT * FROM legacy."RemotePlaylists";

			INSERT INTO "Medias"
			SELECT * FROM legacy."Medias";

			INSERT INTO "Playlists"
			SELECT * FROM legacy."Playlists";

			INSERT INTO "MediaPlayStats" ("MediaId", "PlayCount", "LastPlayed")
			SELECT "Id", 0, '0001-01-01T00:00:00'
			FROM "Medias";

			DETACH DATABASE legacy;

			PRAGMA foreign_keys = ON;
			""";
		command.Parameters.AddWithValue("$legacyPath", backupPath);

		await command.ExecuteNonQueryAsync();
	}
}