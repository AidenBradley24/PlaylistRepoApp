using PlaylistRepoLib;
using PlaylistRepoLib.Models;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace PlaylistRepoAPI
{
	public interface IExportService
	{
		public bool TryGetExport(Guid guid, [NotNullWhen(true)] out ExportRecord? record);
		public Task ExportZip(Playlist playlist, Guid guid, IProgress<TaskProgress> progress);
	}

	public record ExportRecord(Guid Guid, string ExportName, FileInfo? File);

	public class ExportService(IServiceProvider serviceProvider) : IExportService, IDisposable
	{
		private readonly DirectoryInfo tmpDir = Directory.CreateTempSubdirectory();
		private readonly ConcurrentDictionary<Guid, ExportRecord> records = new();

		public async Task ExportZip(Playlist playlist, Guid guid, IProgress<TaskProgress> progress)
		{
			using var scope = serviceProvider.CreateScope();
			using var db = scope.ServiceProvider.GetService<PlayRepoDbContext>()!;
			var file = new FileInfo(Path.Combine(tmpDir.FullName, guid.ToString()));

			if (!records.TryAdd(guid, new ExportRecord(guid, playlist.GenerateFileName("zip"), null)))
				throw new Exception("An error has occured when creating the export record.");

			using (var fs = file.OpenWrite())
			{
				using var zip = new ZipArchive(fs, ZipArchiveMode.Create);
				const string rootMediaPath = "media";
				foreach (var entry in playlist.AllEntries(db.Medias, true))
				{
					FileInfo mediaFile = entry.File ?? throw new Exception("File doesn't exist");
					string mediaPath = Path.Combine(rootMediaPath, mediaFile.Name);
					var zipEntry = zip.CreateEntry(mediaPath);
					using var zipStream = zipEntry.Open();
					using var mediaStream = mediaFile.OpenRead();
					await mediaStream.CopyToAsync(zipStream);
				}
				var playlistFileZipEntry = zip.CreateEntry("playlist.xspf");
				using var playlistZipStream = playlistFileZipEntry.Open();
				await playlist.StreamXspfAsync(db.Medias, playlistZipStream, new PlaylistStreamingSettings() { UseDirectory = true, MediaPath = rootMediaPath });
			}

			records.AddOrUpdate(guid, new ExportRecord(guid, playlist.GenerateFileName("zip"), null), (guid, oldValue) => new ExportRecord(guid, oldValue.ExportName, file));
			progress.Report(TaskProgress.FromCompleted("Export Completed"));
		}

		public bool TryGetExport(Guid guid, [NotNullWhen(true)] out ExportRecord? record)
		{
			return records.TryGetValue(guid, out record);
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			tmpDir.Delete(true);
		}
	}
}
