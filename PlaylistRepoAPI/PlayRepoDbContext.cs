using PlaylistRepoLib;
using PlaylistRepoLib.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace PlaylistRepoAPI;

public class PlayRepoDbContext : DbContext
{
	private readonly IPlayRepoService playRepo;

	public PlayRepoDbContext(IPlayRepoService playRepo)
	{
		this.playRepo = playRepo;
		Database.EnsureCreated();
	}

	public DbSet<Media> Medias { get; set; }

	protected override void OnConfiguring(DbContextOptionsBuilder options)
	{
		options.EnableSensitiveDataLogging();
		options.UseSqlite($"Data Source=\"{Path.Combine(playRepo.DotDir.FullName, "library.db")}\"");
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
	}

	public async Task Ingest(FileInfo[] files, IProgress<TaskProgress> progress)
	{
		int addedCount = 0, updateCount = 0, completedCount = 0;
		Dictionary<byte[], Media> medias = [];
		foreach (FileInfo file in files)
		{
			using var fs = file.OpenRead();
			byte[] hash = await SHA256.HashDataAsync(fs);

			Media media;
			var matching = Medias.Where(m => m.Hash == hash);
			int matchCount = matching.Count();
			if (matchCount == 1)
			{
				media = await matching.FirstAsync();
				media.SyncFromMediaFile();
				media.FilePath = playRepo.GetRelativePath(file);
				updateCount++;
			}
			else if (matchCount > 1 || medias.ContainsKey(hash))
			{
				throw new InvalidOperationException($"Cannot contain multiple identical media.\n{string.Join('\n', matching)}");
			}
			else
			{
				media = new()
				{
					Title = Path.GetFileNameWithoutExtension(file.Name),
					Hash = hash,
					FilePath = playRepo.GetRelativePath(file),
				};
				medias.Add(hash, media);
				addedCount++;
			}

			progress.Report(TaskProgress.FromNumbers(++completedCount, files.Length));
		}

		progress.Report(TaskProgress.FromIndeterminate("Finalizing"));
		await Medias.AddRangeAsync(medias.Values);
		await SaveChangesAsync();
		progress.Report(TaskProgress.FromCompleted($"COMPLETE\n" +
			$"Added {addedCount} new media files.\n" +
			$"Updated {updateCount} media files.\n" +
			string.Join('\n', medias.Values.Select(m => $"ADDED: {m}")) +
			string.Join('\n', medias.Values.Select(m => $"ADDED: {m}"))));
	}
}
