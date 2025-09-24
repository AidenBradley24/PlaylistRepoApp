using Microsoft.EntityFrameworkCore;
using PlaylistRepoLib;
using PlaylistRepoLib.Models;
using System.Diagnostics;

namespace PlaylistRepoAPI
{
	public class YtDlpService(PlayRepoDbContext dbContext, IPlayRepoService repoService) : IRemoteService
	{
		const string OUT_ARG = "-o \"%(id)s;;;%(title)s;;;%(uploader)s;;;%(duration)s;;;%(playlist_index)s;;;%(playlist_id)s\"";
		const string FETCH_ARGS = OUT_ARG + " --skip-download --write-description --write-playlist-metafiles --yes-playlist";
		const string DOWNLOAD_ARGS = OUT_ARG + " --yes-playlist";
		const string SYNC_ARGS = OUT_ARG + " --write-description --write-playlist-metafiles --yes-playlist";

		private static (string uid, string title, string uploader, long? durationMilliseconds, int? order, string? plID) GetAttributes(string fileName)
		{
			string[] attributes = Path.GetFileNameWithoutExtension(fileName).Split(";;;");
			long? durationMilliseconds = attributes[3] == "NA" ? null : long.Parse(attributes[3]) * 1000;
			int? order = attributes[4] == "NA" ? null : int.Parse(attributes[4]);
			string? plID = attributes[5] == "NA" ? null : attributes[5];
			return (attributes[0], attributes[1], attributes[2], durationMilliseconds, order, plID);
		}

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
			process.StartInfo.Arguments = $"\"{remote.Link}\" -P \"{downloadDir.FullName}\" {FETCH_ARGS}";
			process.StartInfo.RedirectStandardOutput = true;
			process.OutputDataReceived += (sender, args) => OutputHandler(sender, args, progress);
			process.Start();
			process.BeginOutputReadLine();
			await process.WaitForExitAsync();

			string? playlistId = null;
			string playlistTitle = "playlist";
			string playlistDescription = "";

			progress?.Report(TaskProgress.FromIndeterminate("Updating database..."));

			if (string.IsNullOrWhiteSpace(remote.MediaMime)) remote.MediaMime = "video/mp4";

			int counter = 0;
			foreach (FileInfo file in downloadDir.EnumerateFiles())
			{
				var (uid, title, uploader, durationSeconds, order, plID) = GetAttributes(file.Name);
				if (plID == null) throw new Exception("yt-dlp downloads outside of playlists are unsupported.");
				playlistId ??= plID;

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
					dbContext.Medias.Add(media);
				}

				media.MimeType = remote.MediaMime;
				media.Title = title;
				media.Description = description;
				if (durationSeconds != null) media.LengthMilliseconds = durationSeconds ?? 0;
				media.Source = remote;
				media.RemoteUID = uid;
				media.RemoteId = remote.Id;
				counter++;
			}

			await dbContext.SaveChangesAsync();
			downloadDir.Delete(true);

			progress?.Report(TaskProgress.FromCompleted($"COMPLETE\n" +
					$"Fetched {counter} media files."));
		}

		public async Task Download(RemotePlaylist remote, IEnumerable<string> mediaUIDs, IProgress<TaskProgress>? progress = null)
		{
			using var process = GetProcessTemplate();
			var downloadDir = Directory.CreateTempSubdirectory();
			string idFilter = "--match-filter " + string.Join('&', mediaUIDs.Select(s => $"id={s}"));
			string format = GetFormat(remote);
			process.StartInfo.Arguments = $"\"{remote.Link}\" -P \"{downloadDir.FullName}\" {idFilter} {format} {DOWNLOAD_ARGS}";
			process.StartInfo.RedirectStandardOutput = true;
			process.OutputDataReceived += (sender, args) => OutputHandler(sender, args, progress);
			process.Start();
			process.BeginOutputReadLine();
			await process.WaitForExitAsync();

			progress?.Report(TaskProgress.FromIndeterminate("Updating database..."));

			List<(FileInfo, Media)> mediaBundles = [];
			foreach (var file in downloadDir.EnumerateFiles())
			{
				var (uid, title, uploader, durationSeconds, order, plID) = GetAttributes(file.Name);
				Media? media = await dbContext.Medias.FirstOrDefaultAsync(m => m.Source == remote && m.RemoteUID == uid);
				if (media == null) continue;
				FileInfo newFile = new(Path.Combine(repoService.RootPath.FullName, media.GenerateFileName(format)));
				File.Copy(file.FullName, newFile.FullName, true);
				mediaBundles.Add((newFile, media));
				media.MimeType = MimeTypes.GetMimeType(newFile.Name);
			}

			await dbContext.IngestExisting([.. mediaBundles], progress);
		}

		public async Task Sync(RemotePlaylist remote, IProgress<TaskProgress>? progress = null)
		{
			using var process = GetProcessTemplate();
			var downloadDir = Directory.CreateTempSubdirectory();
			string format = GetFormat(remote);

			// Filter out existing media files
			string filter = "--match-filter \"" + string.Join('&', dbContext.Medias.Where(m => m.RemoteId == remote.Id && m.FilePath != null)
				.Select(s => $"id!={s}")) + "\"";

			process.StartInfo.Arguments = $"\"{remote.Link}\" -P \"{downloadDir.FullName}\" {format} {filter} {SYNC_ARGS}";
			process.StartInfo.RedirectStandardOutput = true;
			process.OutputDataReceived += (sender, args) => OutputHandler(sender, args, progress);
			process.Start();
			process.BeginOutputReadLine();
			await process.WaitForExitAsync();

			progress?.Report(TaskProgress.FromIndeterminate("Updating database..."));

			string? playlistId = null;
			string playlistTitle = "playlist";
			string playlistDescription = "";

			Dictionary<string, FileInfo> fileMap = [];
			List<Media> newMedias = [];
			List<Media> existingMedias = [];
			foreach (var file in downloadDir.EnumerateFiles())
			{
				var (uid, title, uploader, durationSeconds, order, plID) = GetAttributes(file.Name);
				if (plID == null) throw new Exception("yt-dlp downloads outside of playlists are unsupported.");
				playlistId ??= plID;

				if (file.Name.EndsWith(".description"))
				{
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
							dbContext.Medias.Add(media);
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
						media.RemoteId = remote.Id;

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

			await dbContext.IngestExisting([.. newMedias.Concat(existingMedias).Select(m => (fileMap[m.RemoteUID!], m))], progress);
		}

		private static void OutputHandler(object sender, DataReceivedEventArgs args, IProgress<TaskProgress>? progress)
		{
			if (string.IsNullOrEmpty(args.Data) || progress == null) return;
			if (args.Data.StartsWith("[download] Downloading item "))
			{
				string progText = args.Data["[download] Downloading item ".Length..];
				string[] vals = progText.Split("of");
				int completed = int.Parse(vals[0].Trim());
				int total = int.Parse(vals[1].Trim());
				var progressRecord = TaskProgress.FromNumbers(completed, total, $"downloading data:\n{progText}");
				progress.Report(progressRecord);
			}
		}

		private static string GetFormat(RemotePlaylist remote)
		{
			if (remote.MediaMime == null)
				return $"-f \"bestvideo+bestaudio/best\" --merge-output-format mp4";
			if (remote.MediaMime.StartsWith("video"))
				return $"-f \"bestvideo+bestaudio/best\" --merge-output-format {MimeTypes.GetMimeTypeExtensions(remote.MediaMime).First()} ";
			if (remote.MediaMime.StartsWith("audio"))
				return $"-x --audio-format {MimeTypes.GetMimeTypeExtensions(remote.MediaMime).First()} --output";
			throw new Exception("Type not available " + remote.MediaMime);
		}
	}
}
