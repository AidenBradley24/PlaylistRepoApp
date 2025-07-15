namespace PlaylistRepoAPI
{
	public interface IPlayRepoService
	{
		public DirectoryInfo RootPath { get; set; }
		public DirectoryInfo DotDir { get; }
		public string GetRelativePath(string fullPath);
		public string GetRelativePath(FileInfo file);
	}

	public class PlayRepoService : IPlayRepoService
	{
		public DirectoryInfo RootPath
		{
			get => new(Directory.GetCurrentDirectory());
			set => Directory.SetCurrentDirectory(value.FullName);
		}

		public DirectoryInfo DotDir => RootPath.CreateSubdirectory(".playrepo");

		public PlayRepoService(string rootPath)
		{
			RootPath = new DirectoryInfo(rootPath);
		}

		public string GetRelativePath(string fullPath)
		{
			return Path.GetRelativePath(RootPath.FullName, fullPath);
		}

		public string GetRelativePath(FileInfo file)
		{
			return GetRelativePath(file.FullName);
		}
	}
}
