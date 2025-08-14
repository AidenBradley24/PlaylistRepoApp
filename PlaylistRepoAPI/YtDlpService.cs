using Microsoft.EntityFrameworkCore;
using PlaylistRepoLib;
using PlaylistRepoLib.Models;
using System.Diagnostics;

namespace PlaylistRepoAPI
{
	public class YtDlpService(PlayRepoDbContext dbContext, IPlayRepoService repoService) : IRemoteService
	{
		private static Process GetProcessTemplate()
		{
			var p = new Process();
			p.StartInfo.FileName = "yt-dlp";
			p.StartInfo.CreateNoWindow = true;
			return p;
		}

		public async Task Update(IProgress<TaskProgress>? progress = null)
		{
			using var process = GetProcessTemplate();
			process.StartInfo.Arguments = "--update";
			process.Start();
			await process.WaitForExitAsync();
		}

		public async Task Fetch(RemotePlaylist remote, IProgress<TaskProgress>? progress = null)
		{
			var process = GetProcessTemplate();
			var downloadDir = Directory.CreateTempSubdirectory();
			process.StartInfo.Arguments = $"\"{remote.Link}\" -P \"{downloadDir.FullName}\" --skip-download --write-description --write-playlist-metafiles --yes-playlist";
			process.StartInfo.RedirectStandardOutput = true;
			process.OutputDataReceived += (sender, args) =>
			{
				if (string.IsNullOrEmpty(args.Data) || progress == null) return;
				if (args.Data.StartsWith("[download] Downloading item "))
				{
					string progText = args.Data["[download] Downloading item ".Length..];
					string[] vals = progText.Split("of");
					int completed = int.Parse(vals[0].Trim());
					int total = int.Parse(vals[1].Trim());
					var progressRecord = TaskProgress.FromNumbers(completed, total, $"fetching remote\n{args.Data}");
					progress.Report(progressRecord);
				}
			};
			process.Start();
			process.BeginOutputReadLine();
			await process.WaitForExitAsync();

			string playlistId = GetPlaylistId(remote.Link);

			List<Media> newMedias = [];
			string playlistTitle = "playlist";
			string playlistDescription = "";

			progress?.Report(TaskProgress.FromIndeterminate("Updating database..."));

			int counter = 0;
			foreach (FileInfo file in downloadDir.EnumerateFiles())
			{
				string uid = GetURLTag(file.Name);

				string title = GetTitleWithoutURLTag(file.Name);
				using var reader = file.OpenText();
				string description = await reader.ReadToEndAsync();

				if (uid == playlistId)
				{
					playlistTitle = title;
					playlistDescription = description;
					continue;
				}

				Media? media = await dbContext.Medias.FirstOrDefaultAsync(m => m.RemoteUID == uid);
				if (media?.Locked ?? false) continue;

				if (media == null)
				{
					media = new();
					newMedias.Add(media);
				}

				media.Title = title;
				media.Description = description;
				media.Source = remote;
				media.RemoteUID = uid;

				// TODO metadata filler here

				counter++;
			}

			dbContext.Medias.AddRange(newMedias);
			await dbContext.SaveChangesAsync();
			downloadDir.Delete(true);

			progress?.Report(TaskProgress.FromCompleted($"COMPLETE\n" +
					$"Fetched {counter} media files.\n" +
					string.Join('\n', newMedias.Select(m => $"ADDED: {m.Title}"))));
		}

		public async Task Download(RemotePlaylist remote, IEnumerable<string> mediaUIDs, string? downloadFormat = null, IProgress<TaskProgress>? progress = null)
		{
			using var process = GetProcessTemplate();
			var downloadDir = Directory.CreateTempSubdirectory();
			string idFilter = "--match-filter " + string.Join('&', mediaUIDs.Select(s => $"id={s}"));
			string extension = downloadFormat != null ? "--format " + downloadFormat : "";
			process.StartInfo.Arguments = $"\"{remote.Link}\" -P \"{downloadDir.FullName}\" --yes-playlist {idFilter} {extension}";
			process.StartInfo.RedirectStandardOutput = true;
			
			process.OutputDataReceived += (sender, args) =>
			{
				Console.WriteLine(args.Data);
				if (string.IsNullOrEmpty(args.Data) || progress == null) return;
				if (args.Data.StartsWith("[download] Downloading item "))
				{
					string progText = args.Data["[download] Downloading item ".Length..];
					string[] vals = progText.Split("of");
					int completed = int.Parse(vals[0].Trim());
					int total = int.Parse(vals[1].Trim());
					var progressRecord = TaskProgress.FromNumbers(completed, total, $"downloading remote\n{args.Data}");
					progress.Report(progressRecord);
				}
			};
			process.Start();
			process.BeginOutputReadLine();
			await process.WaitForExitAsync();

			progress?.Report(TaskProgress.FromIndeterminate("Updating database..."));

			List<(FileInfo, Media)> mediaBundles = [];
			foreach(var file in downloadDir.EnumerateFiles())
			{
				string uid = GetURLTag(file.Name);
				Media? media = await dbContext.Medias.FirstOrDefaultAsync(m => m.Source == remote && m.RemoteUID == uid);
				if (media == null) continue;
				FileInfo newFile = new(Path.Combine(repoService.RootPath.FullName, media.GenerateFileName(extension)));
				File.Copy(file.FullName, newFile.FullName, true);
				mediaBundles.Add((newFile, media));
			}

			await dbContext.IngestExisting([.. mediaBundles], progress);
		}

		public async Task Sync(RemotePlaylist remote, string? downloadFormat = null, IProgress<TaskProgress>? progress = null)
		{
			using var process = GetProcessTemplate();
			var downloadDir = Directory.CreateTempSubdirectory();
			string extension = downloadFormat != null ? "--format " + downloadFormat : "";

			// Filter out existing media files
			string filter = "--match-filter \"" + string.Join('&', dbContext.Medias.Where(m => m.RemoteId == remote.Id && m.FilePath != null)
				.Select(s => $"id!={s}")) + "\"";

			process.StartInfo.Arguments = $"\"{remote.Link}\" -P \"{downloadDir.FullName}\" -f bestaudio --write-description --write-playlist-metafiles --yes-playlist {extension} {filter}";
			process.StartInfo.RedirectStandardOutput = true;

			process.OutputDataReceived += (sender, args) =>
			{
				if (string.IsNullOrEmpty(args.Data) || progress == null) return;
				if (args.Data.StartsWith("[download] Downloading item "))
				{
					string progText = args.Data["[download] Downloading item ".Length..];
					string[] vals = progText.Split("of");
					int completed = int.Parse(vals[0].Trim());
					int total = int.Parse(vals[1].Trim());
					var progressRecord = TaskProgress.FromNumbers(completed, total, $"downloading remote\n{args.Data}");
					progress.Report(progressRecord);
				}
			};
			process.Start();
			process.BeginOutputReadLine();
			await process.WaitForExitAsync();

			progress?.Report(TaskProgress.FromIndeterminate("Updating database..."));

			string playlistId = GetPlaylistId(remote.Link);
			string playlistTitle = "playlist";
			string playlistDescription = "";

			Dictionary<string, FileInfo> fileMap = [];
			List<Media> newMedias = [];
			List<Media> existingMedias = [];
			foreach (var file in downloadDir.EnumerateFiles())
			{
				string uid = GetURLTag(file.Name);
				if (file.Name.EndsWith(".description"))
				{
					string title = GetTitleWithoutURLTag(file.Name);
					using (var reader = file.OpenText())
					{
						string description = await reader.ReadToEndAsync();

						if (uid == playlistId)
						{
							playlistTitle = title;
							playlistDescription = description;
							continue;
						}

						Media? media = await dbContext.Medias.FirstOrDefaultAsync(m => m.RemoteUID == uid);
						if (media?.Locked ?? false) continue;

						if (media == null)
						{
							media = new();
							newMedias.Add(media);
						}
						else
						{
							existingMedias.Add(media);
						}

						media.Title = title;
						media.Description = description;
						media.Source = remote;
						media.RemoteUID = uid;

						// TODO add metadata fillers
					}

					file.Delete();
				}
				else
				{
					// audio file
					fileMap.Add(uid, file);
				}				
			}

			await dbContext.Medias.AddRangeAsync(newMedias);
			await dbContext.IngestExisting([.. newMedias.Concat(existingMedias).Select(m => (fileMap[m.RemoteUID!], m))], progress);
		}

		public static string GetTitleWithoutURLTag(string fileName)
		{
			return fileName[..fileName.LastIndexOf('[')].Trim();
		}

		public static string GetURLTag(string fileName)
		{
			return fileName[(fileName.LastIndexOf('[') + 1)..fileName.LastIndexOf(']')];
		}

		public static string GetPlaylistId(string url)
		{
			string playlistID;
			const string LIST_URL = "list=";
			if (url.Contains('&'))
			{
				playlistID = url.Split('&').Where(s => s.StartsWith(LIST_URL)).First()[LIST_URL.Length..];
			}
			else
			{
				int listIndex = url.IndexOf(LIST_URL) + LIST_URL.Length;
				playlistID = url[listIndex..];
			}
			return playlistID;
		}
	}
}
