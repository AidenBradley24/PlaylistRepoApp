using Microsoft.EntityFrameworkCore.Design;

namespace PlaylistRepoAPI;

public sealed class PlayRepoDbContextFactory : IDesignTimeDbContextFactory<PlayRepoDbContext>
{
	public PlayRepoDbContext CreateDbContext(string[] args)
	{
		string repoRoot = Path.Combine(Path.GetTempPath(), "PlaylistRepoApp.EFDesign");
		Directory.CreateDirectory(repoRoot);

		var repo = new PlayRepoService(repoRoot);
		repo.Initialize();

		return new PlayRepoDbContext(repo);
	}
}