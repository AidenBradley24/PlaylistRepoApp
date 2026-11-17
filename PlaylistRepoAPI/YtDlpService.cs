using Microsoft.EntityFrameworkCore;
using PlaylistRepoLib;
using PlaylistRepoLib.Models;
using PlaylistRepoLib.Models.DTOs;
using System.Diagnostics;
using System.Text.Json;

namespace PlaylistRepoAPI
{
	public class YtDlpService(PlayRepoDbContext dbContext, IPlayRepoService repoService) : IRemoteService
	{
		const string OUT_TEMPLATE = "%(id)s.%(ext)s";

		private sealed record MediaMetadata(string Id, string Title, string Description, long DurationMilliseconds, int Order);

		private static async Task<MediaMetadata> ReadMetadata(FileInfo file)
		{
			using var stream = file.OpenRead();
			using var json = await JsonDocument.ParseAsync(stream);
			var root = json.RootElement;
			string? ReadString(string name) => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
			var id = ReadString("id") ?? throw new InvalidDataException($"Missing media ID in yt-dlp metadata: {file.Name}");
			var duration = root.TryGetProperty("duration", out var durationValue) && durationValue.ValueKind == JsonValueKind.Number && durationValue.TryGetDouble(out var seconds) ? (long)(seconds * 1000) : 0;
			var order = root.TryGetProperty("playlist_index", out var indexValue) && indexValue.ValueKind == JsonValueKind.Number && indexValue.TryGetInt32(out var index) ? index : 0;
			return new(id, ReadString("title") ?? id, ReadString("description") ?? "", duration, order);
		}

		private static FileInfo FindMediaFile(DirectoryInfo directory, FileInfo metadataFile)
		{
			var name = metadataFile.Name[..^".info.json".Length];
			return directory.EnumerateFiles().SingleOrDefault(file => Path.GetFileNameWithoutExtension(file.Name) == name && file.Extension != ".json")
				?? throw new InvalidDataException($"yt-dlp did not produce a media file for '{name}'.");
		}

		private static Process GetProcessTemplate()
		{
			var p = new Process();
			p.StartInfo.FileName = "yt-dlp";
			p.StartInfo.CreateNoWindow = true;
			return p;
		}

		private static void ConfigureProcess(Process process, RemotePlaylist remote, DirectoryInfo directory, params string[] arguments)
		{
			process.StartInfo.ArgumentList.Add(remote.Link);
			process.StartInfo.ArgumentList.Add("-P");
			process.StartInfo.ArgumentList.Add(directory.FullName);
			process.StartInfo.ArgumentList.Add("-o");
			process.StartInfo.ArgumentList.Add(OUT_TEMPLATE);
			process.StartInfo.ArgumentList.Add("--yes-playlist");
			process.StartInfo.ArgumentList.Add("--write-info-json");
			process.StartInfo.ArgumentList.Add("--no-write-playlist-metafiles");
			foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
		}

		private static async Task RunProcess(Process process, IProgress<TaskProgress>? progress)
		{
			process.OutputDataReceived += (sender, args) => OutputHandler(sender, args, progress);
			process.Start();
			process.BeginOutputReadLine();
			var errors = process.StandardError.ReadToEndAsync();
			await process.WaitForExitAsync();
			var error = (await errors).Trim();
			if (process.ExitCode != 0)
				throw new InvalidOperationException($"yt-dlp failed (exit code {process.ExitCode}){(error.Length > 0 ? $": {error}" : ". Check the remote URL and yt-dlp installation.")}");
		}

		private static string FilterValue(string value) => "'" + value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("&", "\\&") + "'";

		public async Task Update(IProgress<TaskProgress>? progress = null)
		{
			using var process = GetProcessTemplate();
			process.StartInfo.Arguments = "--update";
			process.Start();
			await process.WaitForExitAsync();
		}


		public async Task Fetch(RemotePlaylist remote, IProgress<TaskProgress>? progress = null)
		{
			using var process = GetProcessTemplate();
			var downloadDir = Directory.CreateTempSubdirectory();
			try
			{
				ConfigureProcess(process, remote, downloadDir, "--skip-download");
				await RunProcess(process, progress);
				progress?.Report(TaskProgress.FromIndeterminate("Updating database..."));

				int counter = 0;
				foreach (var file in downloadDir.EnumerateFiles("*.info.json"))
				{
					var metadata = await ReadMetadata(file);
					Media? media = await remote.AllEntries(dbContext.Medias).FirstOrDefaultAsync(m => m.RemoteUID == metadata.Id);
					if (media?.Locked ?? false) continue;
					if (media == null)
					{
						media = new();
						dbContext.Medias.Add(media);
					}
					await ApplyMetadata(media, remote, metadata);
					if (string.IsNullOrWhiteSpace(media.MimeType)) media.MimeType = string.IsNullOrWhiteSpace(remote.MediaMime) ? "video/mp4" : remote.MediaMime;
					counter++;
				}
				await dbContext.SaveChangesAsync();
				progress?.Report(TaskProgress.FromCompleted($"Fetched {counter} media files."));
			}
			finally
			{
				downloadDir.Delete(true);
			}
		}

		private static async Task ApplyMetadata(Media media, RemotePlaylist remote, MediaMetadata metadata)
		{
			media.Title = metadata.Title;
			media.Description = metadata.Description;
			media.RemoteUID = metadata.Id;
			media.RemoteId = remote.Id;
			media.LengthMilliseconds = metadata.DurationMilliseconds;
			media.Order = metadata.Order;
			var enricher = new MediaHeuristicMetadataEnricher();
			var dto = await enricher.TryEnrich(media.GetDTO());
			dto?.UpdateModel(media);
		}

		public async Task Download(RemotePlaylist remote, IEnumerable<string> mediaUIDs, IProgress<TaskProgress>? progress = null)
		{
			var ids = mediaUIDs.Distinct().ToArray();
			if (ids.Length == 0)
			{
				progress?.Report(TaskProgress.FromCompleted("No unlocked media matched the download filter. Fetch the remote first if it has no entries."));
				return;
			}
			using var process = GetProcessTemplate();
			var downloadDir = Directory.CreateTempSubdirectory();
			try
			{
				ConfigureProcess(process, remote, downloadDir, GetFormat(remote));
				foreach (var uid in ids)
				{
					process.StartInfo.ArgumentList.Add("--match-filters");
					process.StartInfo.ArgumentList.Add($"id = {FilterValue(uid)}");
				}
				await RunProcess(process, progress);
				progress?.Report(TaskProgress.FromIndeterminate("Updating database..."));

				List<(FileInfo, Media)> mediaBundles = [];
				HashSet<string> downloadedIds = [];
				foreach (var metadataFile in downloadDir.EnumerateFiles("*.info.json"))
				{
					var metadata = await ReadMetadata(metadataFile);
					Media? media = await remote.AllEntries(dbContext.Medias).FirstOrDefaultAsync(m => m.RemoteUID == metadata.Id);
					if (media == null || media.Locked) continue;
					var file = FindMediaFile(downloadDir, metadataFile);
					FileInfo newFile = new(media.GetAvailableDownloadPath(repoService.RootPath.FullName, file.Extension));
					File.Copy(file.FullName, newFile.FullName);
					mediaBundles.Add((newFile, media));
					downloadedIds.Add(metadata.Id);
				}

				await dbContext.IngestExisting([.. mediaBundles], null);
				var missing = ids.Except(downloadedIds).ToArray();
				if (missing.Length > 0)
					progress?.Report(TaskProgress.FromError($"Downloaded {mediaBundles.Count} item(s), but these requested IDs were not downloaded: {string.Join(", ", missing)}."));
				else
					progress?.Report(TaskProgress.FromCompleted($"Downloaded {mediaBundles.Count} media files."));
			}
			finally
			{
				downloadDir.Delete(true);
			}
		}

		public async Task Sync(RemotePlaylist remote, IProgress<TaskProgress>? progress = null)
		{
			using var process = GetProcessTemplate();
			var downloadDir = Directory.CreateTempSubdirectory();
			try
			{
				ConfigureProcess(process, remote, downloadDir, GetFormat(remote));
				var existing = await dbContext.Medias.Where(m => m.RemoteId == remote.Id && m.RemoteUID != null)
					.Select(m => new { m.RemoteUID, m.FilePath, m.Locked }).ToArrayAsync();
				var excludedIds = existing.Where(m => m.Locked || (m.FilePath != null && File.Exists(Path.Combine(repoService.RootPath.FullName, m.FilePath))))
					.Select(m => m.RemoteUID!).ToArray();
				if (excludedIds.Length > 0)
				{
					process.StartInfo.ArgumentList.Add("--match-filters");
					process.StartInfo.ArgumentList.Add(string.Join(" & ", excludedIds.Select(uid => $"id != {FilterValue(uid)}")));
				}
				await RunProcess(process, progress);
				progress?.Report(TaskProgress.FromIndeterminate("Updating database..."));

				List<(FileInfo, Media)> bundles = [];
				foreach (var metadataFile in downloadDir.EnumerateFiles("*.info.json"))
				{
					var metadata = await ReadMetadata(metadataFile);
					Media? media = await remote.AllEntries(dbContext.Medias).FirstOrDefaultAsync(m => m.RemoteUID == metadata.Id);
					if (media?.Locked ?? false) continue;
					var file = FindMediaFile(downloadDir, metadataFile);
					if (media == null)
					{
						media = new();
						dbContext.Medias.Add(media);
					}
					await ApplyMetadata(media, remote, metadata);
					var path = media.GetAvailableDownloadPath(repoService.RootPath.FullName, file.Extension);
					file.MoveTo(path);
					bundles.Add((file, media));
				}
				await dbContext.IngestExisting([.. bundles], null);
				progress?.Report(TaskProgress.FromCompleted($"Synced {bundles.Count} media files."));
			}
			finally
			{
				downloadDir.Delete(true);
			}
		}

		private static void OutputHandler(object sender, DataReceivedEventArgs args, IProgress<TaskProgress>? progress)
		{
			if (string.IsNullOrEmpty(args.Data) || progress == null) return;
			if (args.Data.StartsWith("[download] Downloading item "))
			{
				string progText = args.Data["[download] Downloading item ".Length..];
				string[] vals = progText.Split("of");
				if (vals.Length != 2 || !int.TryParse(vals[0].Trim(), out var completed) || !int.TryParse(vals[1].Trim(), out var total) || total <= 0) return;
				var progressRecord = TaskProgress.FromNumbers(completed, total, $"downloading data:\n{progText}");
				progress.Report(progressRecord);
			}
		}

		private static string[] GetFormat(RemotePlaylist remote)
		{
			if (string.IsNullOrWhiteSpace(remote.MediaMime))
				return ["-f", "bestvideo+bestaudio/best", "--merge-output-format", "mp4"];
			if (remote.MediaMime.StartsWith("video"))
				return ["-f", "bestvideo+bestaudio/best", "--merge-output-format", MimeTypes.GetMimeTypeExtensions(remote.MediaMime).First().TrimStart('.')];
			if (remote.MediaMime.StartsWith("audio"))
				return ["-x", "--audio-format", MimeTypes.GetMimeTypeExtensions(remote.MediaMime).First().TrimStart('.')];
			throw new Exception("Type not available " + remote.MediaMime);
		}
	}
}
