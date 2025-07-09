using LocalPlaylistMasterLib;
using Microsoft.EntityFrameworkCore;

namespace LocalPlaylistMasterAPI;

public class PlayRepoDbContext : DbContext
{
	public DirectoryInfo RootPath { get; }
	public DirectoryInfo DotDir { get => RootPath.CreateSubdirectory(".playrepo"); }

	public DbSet<Media> Media { get; set; }

	public PlayRepoDbContext(DbContextOptions<PlayRepoDbContext> options, string rootPath) : base(options)
	{
		RootPath = Directory.CreateDirectory(rootPath);
		DotDir.Create();
	}

	protected override void OnConfiguring(DbContextOptionsBuilder options)
	{
		options.EnableSensitiveDataLogging();
		options.UseSqlite($"Data Source=\"{Path.Combine(DotDir.FullName, "library.db")}\"");
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
	}
}
