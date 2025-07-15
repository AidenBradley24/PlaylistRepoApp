using System.Collections;

namespace PlaylistRepoLib
{
	/// <summary>
	/// Includes all files recursively inside of <paramref name="src"/>. Excludes files inside of dot directories.
	/// </summary>
	/// <param name="src">Search specification, supports wildcards</param>
	public class FileSpec(string src) : IEnumerable<FileInfo>
	{
		public IEnumerator<FileInfo> GetEnumerator()
		{
			string? directory = Path.GetDirectoryName(src);
			string searchPattern = Path.GetFileName(src) ?? "";

			if (string.IsNullOrEmpty(directory))
			{
				directory = Directory.GetCurrentDirectory();
			}

			IEnumerable<FileInfo> EnumerateFiles(string root)
			{
				foreach (string file in Directory.EnumerateFiles(root, searchPattern))
				{
					yield return new FileInfo(file);
				}

				foreach (string subdir in Directory.EnumerateDirectories(root))
				{
					if (Path.GetFileName(subdir).StartsWith(".", StringComparison.OrdinalIgnoreCase))
						continue;

					foreach (var file in EnumerateFiles(subdir))
					{
						yield return file;
					}
				}
			}

			return EnumerateFiles(directory).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
