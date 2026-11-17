using Microsoft.EntityFrameworkCore;
using PlaylistRepoLib;
using PlaylistRepoLib.Models;
using PlaylistRepoLib.Models.DTOs;

namespace PlaylistRepoAPI
{
	/// <summary>
	/// Direct downloads over HTTP. Parses playlist files and interprets media references.
	/// </summary>
	public class InternetRemoteService(HttpClient http, PlayRepoDbContext dbContext, IPlayRepoService repoService) : IRemoteService
	{
		public async Task Download(RemotePlaylist remote, IEnumerable<string> mediaUIDs, IProgress<TaskProgress>? progress = null)
		{
			progress?.Report(TaskProgress.FromIndeterminate("Starting download..."));

			// Fetch remote playlist file and parse media references
			var request = new HttpRequestMessage(HttpMethod.Get, remote.Link);
			var response = await http.SendAsync(request);
			if (!response.IsSuccessStatusCode)
			{
				progress?.Report(TaskProgress.FromError($"Failed to fetch remote playlist: {response.StatusCode}"));
				return;
			}
			var stream = await response.Content.ReadAsStreamAsync();
			var mediaRefs = await ParseFile(stream, Path.GetFileName(new Uri(remote.Link).AbsolutePath), remote);

			// Filter mediaRefs by requested UIDs
			var requestedRefs = mediaRefs.Where(m => m.RemoteUID != null && mediaUIDs.Contains(m.RemoteUID)).ToList();
			int total = requestedRefs.Count, completed = 0;
			List<string> errors = [];

			foreach (var mediaRef in requestedRefs)
			{
				Media? media = await remote.AllEntries(dbContext.Medias)
					.FirstOrDefaultAsync(m => m.RemoteUID == mediaRef.RemoteUID);

				if (media?.Locked ?? false)
				{
					completed++;
					continue;
				}

				// Download media file
				try
				{
					var mediaUri = new Uri(new Uri(remote.Link), mediaRef.FilePath ?? remote.Link);
					if (mediaUri.Scheme != Uri.UriSchemeHttp && mediaUri.Scheme != Uri.UriSchemeHttps)
						throw new InvalidOperationException("Media URL must use HTTP or HTTPS.");
					var mediaRequest = new HttpRequestMessage(HttpMethod.Get, mediaUri);
					var mediaResponse = await http.SendAsync(mediaRequest);
					if (!mediaResponse.IsSuccessStatusCode)
					{
						errors.Add($"'{mediaRef.Title}': HTTP {(int)mediaResponse.StatusCode} {mediaResponse.ReasonPhrase}");
						continue;
					}

					var fileBytes = await mediaResponse.Content.ReadAsByteArrayAsync();
					var directory = Path.Combine(repoService.RootPath.FullName, "media");
					Directory.CreateDirectory(directory);
					var filePath = mediaRef.GetAvailableDownloadPath(directory, Path.GetExtension(mediaUri.AbsolutePath));
					await using (var file = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
						await file.WriteAsync(fileBytes);

					if (media == null)
					{
						media = new();
						dbContext.Medias.Add(media);
					}

					var dto = new MediaDTO(mediaRef) { Id = media.Id };
					dto.UpdateModel(media);
					media.RemoteId = remote.Id;
					media.RemoteUID = mediaRef.RemoteUID;
					media.FilePath = repoService.GetRelativePath(filePath);
				}
				catch (Exception ex)
				{
					errors.Add($"'{mediaRef.Title}': {ex.Message}");
					continue;
				}

				completed++;
				progress?.Report(TaskProgress.FromNumbers(completed, total, $"Downloaded {completed}/{total}"));
			}

			await dbContext.SaveChangesAsync();
			if (errors.Count > 0)
			{
				progress?.Report(TaskProgress.FromError($"Failed to download {errors.Count} item(s): {string.Join("; ", errors)}"));
				return;
			}
			progress?.Report(TaskProgress.FromCompleted("Download completed."));
		}

		public async Task Fetch(RemotePlaylist remote, IProgress<TaskProgress>? progress = null)
		{
			var request = new HttpRequestMessage(HttpMethod.Get, remote.Link);
			progress?.Report(TaskProgress.FromIndeterminate("Fetching file..."));
			var response = await http.SendAsync(request);
			if (!response.IsSuccessStatusCode)
			{
				progress?.Report(TaskProgress.FromError($"Request message failed '{response.StatusCode}'"));
				return;
			}
			var stream = await response.Content.ReadAsStreamAsync();
			var mediaRefs = await ParseFile(stream, Path.GetFileName(new Uri(remote.Link).AbsolutePath), remote);

			foreach (var mediaRef in mediaRefs)
			{
				Media? media = await remote.AllEntries(dbContext.Medias).FirstOrDefaultAsync(m => m.RemoteUID == mediaRef.RemoteUID);
				if (media?.Locked ?? false) continue;

				if (media == null)
				{
					media = new();
					dbContext.Medias.Add(media);
				}

				var dto = new MediaDTO(mediaRef)
				{
					Id = media.Id
				};

				dto.UpdateModel(media);
				media.RemoteId = remote.Id;
				media.RemoteUID = mediaRef.RemoteUID;
			}

			await dbContext.SaveChangesAsync();
			progress?.Report(TaskProgress.FromCompleted());
		}

		public async Task Sync(RemotePlaylist remote, IProgress<TaskProgress>? progress = null)
		{
			progress?.Report(TaskProgress.FromIndeterminate("Syncing remote playlist..."));

			// Fetch and parse all media from remote
			var request = new HttpRequestMessage(HttpMethod.Get, remote.Link);
			var response = await http.SendAsync(request);
			if (!response.IsSuccessStatusCode)
			{
				progress?.Report(TaskProgress.FromError($"Failed to fetch remote playlist: {response.StatusCode}"));
				return;
			}
			var stream = await response.Content.ReadAsStreamAsync();
			var mediaRefs = await ParseFile(stream, Path.GetFileName(new Uri(remote.Link).AbsolutePath), remote);

			// Only unlocked media without an existing local file
			var unlockedRefs = new List<Media>();
			foreach (var mediaRef in mediaRefs)
			{
				Media? media = await remote.AllEntries(dbContext.Medias)
					.FirstOrDefaultAsync(m => m.RemoteUID == mediaRef.RemoteUID);

				if (media?.Locked == true || (media?.FilePath != null && File.Exists(Path.Combine(repoService.RootPath.FullName, media.FilePath))))
					continue;

				unlockedRefs.Add(mediaRef);
			}

			// Download all unlocked media
			await Download(remote, unlockedRefs.Select(m => m.RemoteUID!).Where(uid => uid != null), progress);
		}

		public Task Update(IProgress<TaskProgress>? progress = null)
		{
			progress?.Report(TaskProgress.FromCompleted());
			return Task.CompletedTask;
		}

		private static async Task<List<Media>> ParseFile(Stream file, string filename, RemotePlaylist remote)
		{
			Playlist playlist;
			List<Media> media;

			remote.Name = Path.GetFileNameWithoutExtension(filename);

			string extension = Path.GetExtension(filename).ToLower();
			if (extension.Equals(".xspf"))
			{
				(playlist, media) = await PlaylistExtensions.ParseXspfAsync(file);
			}
			else if (extension.Equals(".m3u8"))
			{
				(playlist, media) = await PlaylistExtensions.ParseM3U8Async(file);
			}
			else if (extension.Equals(".csv"))
			{
				(playlist, media) = await PlaylistExtensions.ParseCSVAsync(file, ',');
			}
			else
			{
				remote.MediaMime = MimeTypes.GetMimeType(extension);
				Media mediaDTO = new()
				{
					Title = remote.Name,
					MimeType = remote.MediaMime,
					RemoteUID = remote.Link,
					FilePath = remote.Link,
				};
				return [mediaDTO];
			}

			return media;
		}
	}
}
