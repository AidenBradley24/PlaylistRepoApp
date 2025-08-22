using System.Diagnostics.CodeAnalysis;

namespace PlaylistRepoAPI
{
	public interface IPlayRepoService
	{
		[MemberNotNullWhen(true, nameof(DotDir))]
		public bool IsRepoInitialized { get; }
		public DirectoryInfo RootPath { get; set; }
		public DirectoryInfo? DotDir { get; }
		public string GetRelativePath(string fullPath);
		public string GetRelativePath(FileInfo file);

		public bool Initialize();
	}

	public class PlayRepoService : IPlayRepoService
	{
		public bool IsRepoInitialized { get => Directory.Exists(Path.Combine(RootPath.FullName, ".playrepo")); }

		public DirectoryInfo RootPath
		{
			get => new(Directory.GetCurrentDirectory());
			set => Directory.SetCurrentDirectory(value.FullName);
		}

		public DirectoryInfo? DotDir => IsRepoInitialized ? RootPath.CreateSubdirectory(".playrepo") : null;

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

		public bool Initialize()
		{
			if (IsRepoInitialized) return false;
			RootPath.CreateSubdirectory(".playrepo");
			return true;
		}
	}
}
