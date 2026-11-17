using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PlaylistRepoAPI;
using PlaylistRepoAPI.Controllers;
using PlaylistRepoLib;
using PlaylistRepoLib.Models;

namespace Tests;

public class RemoteHardeningTests
{
	[Fact]
	public async Task LockedUndownloadedCheckCountsOnlyAffectedMediaInSelectedRemote()
	{
		using var fixture = new Fixture();
		var remote = fixture.AddRemote();
		var otherRemote = fixture.AddRemote();
		await File.WriteAllTextAsync(Path.Combine(fixture.Repo.RootPath.FullName, "downloaded.mp3"), "media");
		fixture.Db.Medias.AddRange(
			new Media { RemoteId = remote.Id, Locked = true, FilePath = null },
			new Media { RemoteId = remote.Id, Locked = true, FilePath = "missing.mp3" },
			new Media { RemoteId = remote.Id, Locked = true, FilePath = "downloaded.mp3" },
			new Media { RemoteId = remote.Id, Locked = false, FilePath = null },
			new Media { RemoteId = otherRemote.Id, Locked = true, FilePath = null });
		await fixture.Db.SaveChangesAsync();
		var controller = new DataController(fixture.Db);

		var result = Assert.IsType<OkObjectResult>(await controller.GetLockedUndownloadedMedia(remote.Id, fixture.Repo));
		Assert.Equal(2, JsonSerializer.SerializeToElement(result.Value).GetProperty("count").GetInt32());

		var otherResult = Assert.IsType<OkObjectResult>(await controller.GetLockedUndownloadedMedia(otherRemote.Id, fixture.Repo));
		Assert.Equal(1, JsonSerializer.SerializeToElement(otherResult.Value).GetProperty("count").GetInt32());
		Assert.IsType<NotFoundResult>(await controller.GetLockedUndownloadedMedia(otherRemote.Id + 1, fixture.Repo));
	}

	[Fact]
	public async Task LockedUndownloadedCheckReturnsZeroWhenNothingIsAffected()
	{
		using var fixture = new Fixture();
		var remote = fixture.AddRemote();
		fixture.Db.Medias.Add(new Media { RemoteId = remote.Id, Locked = false });
		await fixture.Db.SaveChangesAsync();

		var result = Assert.IsType<OkObjectResult>(await new DataController(fixture.Db).GetLockedUndownloadedMedia(remote.Id, fixture.Repo));
		Assert.Equal(0, JsonSerializer.SerializeToElement(result.Value).GetProperty("count").GetInt32());
	}

	public static bool YtDlpIntegrationEnabled => Environment.GetEnvironmentVariable("PLAYLISTREPO_TEST_YTDLP") == "1";

	[Fact(Skip = "Set PLAYLISTREPO_TEST_YTDLP=1 to run the local yt-dlp integration test.", SkipUnless = nameof(YtDlpIntegrationEnabled))]
	public async Task YtDlpFetchDownloadAndSyncHandleDescriptionlessPlaylist()
	{
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseUrls("http://127.0.0.1:0");
		await using var app = builder.Build();
		app.MapGet("/index.html", () => Results.Content("<html><head><title>Test playlist</title></head><body><video src='/one.wav'></video><video src='/two.wav'></video></body></html>", "text/html"));
		app.MapGet("/{name}.wav", () => Results.File(CreateWave(), "audio/wav"));
		await app.StartAsync();
		var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
		using var fixture = new Fixture();
		var remote = fixture.AddRemote();
		remote.Link = address + "/index.html";
		await fixture.Db.SaveChangesAsync();
		var service = new YtDlpService(fixture.Db, fixture.Repo);
		var progress = new RecordingProgress();

		await service.Fetch(remote, progress);
		var media = await fixture.Db.Medias.OrderBy(m => m.Order).ToArrayAsync();
		Assert.Equal(2, media.Length);
		Assert.All(media, m => Assert.Null(m.FilePath));
		await service.Download(remote, media.Select(m => m.RemoteUID!), progress);
		Assert.False(progress.Last!.IsError, progress.Last.Status);
		Assert.All(media, m => Assert.True(File.Exists(Path.Combine(fixture.Repo.RootPath.FullName, m.FilePath!))));

		File.Delete(Path.Combine(fixture.Repo.RootPath.FullName, media[0].FilePath!));
		media[1].Locked = true;
		await fixture.Db.SaveChangesAsync();
		await service.Sync(remote, progress);
		Assert.False(progress.Last!.IsError, progress.Last.Status);
		Assert.True(File.Exists(Path.Combine(fixture.Repo.RootPath.FullName, media[0].FilePath!)));
		Assert.Equal(2, await fixture.Db.Medias.CountAsync());

		var newRemote = new RemotePlaylist { Link = remote.Link };
		fixture.Db.RemotePlaylists.Add(newRemote);
		await fixture.Db.SaveChangesAsync();
		await service.Sync(newRemote, progress);
		Assert.False(progress.Last!.IsError, progress.Last.Status);
		Assert.Equal(2, await fixture.Db.Medias.CountAsync(m => m.RemoteId == newRemote.Id && m.FilePath != null));
		var paths = await fixture.Db.Medias.Select(m => m.FilePath).ToArrayAsync();
		Assert.Equal(paths.Length, paths.Distinct().Count());

		newRemote.Link = address + "/missing";
		var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.Fetch(newRemote, progress));
		Assert.Contains("yt-dlp failed (exit code", error.Message);
	}

	private static byte[] CreateWave()
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		writer.Write(Encoding.ASCII.GetBytes("RIFF"));
		writer.Write(36 + 8000);
		writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
		writer.Write(16);
		writer.Write((short)1);
		writer.Write((short)1);
		writer.Write(8000);
		writer.Write(8000);
		writer.Write((short)1);
		writer.Write((short)8);
		writer.Write(Encoding.ASCII.GetBytes("data"));
		writer.Write(8000);
		writer.Write(new byte[8000]);
		return stream.ToArray();
	}

	[Fact]
	public async Task DownloadHttpRequestAcceptsAnEmptyQueryHeader()
	{
		using var fixture = new Fixture();
		var remote = fixture.AddRemote();
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseUrls("http://127.0.0.1:0");
		builder.Services.AddSingleton(fixture.Db);
		builder.Services.AddSingleton<IRemoteService, RecordingRemoteService>();
		builder.Services.AddSingleton<ITaskService, TaskService>();
		builder.Services.AddControllers().AddApplicationPart(typeof(ActionController).Assembly);
		await using var app = builder.Build();
		app.MapControllers();
		await app.StartAsync();
		var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
		using var client = new HttpClient { BaseAddress = new Uri(address) };
		using var request = new HttpRequestMessage(HttpMethod.Post, "/api/action/download");
		request.Headers.Add("remoteId", remote.Id.ToString());
		request.Headers.Add("query", "");

		using var response = await client.SendAsync(request);

		Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
	}

	[Fact]
	public async Task DownloadAcceptsAnEmptyFilterAndSelectsUnlockedMedia()
	{
		using var fixture = new Fixture();
		var remote = fixture.AddRemote();
		fixture.Db.Medias.AddRange(
			new Media { RemoteId = remote.Id, RemoteUID = "one" },
			new Media { RemoteId = remote.Id, RemoteUID = "locked", Locked = true });
		await fixture.Db.SaveChangesAsync();
		var downloader = new RecordingRemoteService();
		using var services = new ServiceCollection().AddSingleton<IRemoteService>(downloader).BuildServiceProvider();
		var tasks = new TaskService(NullLogger<TaskService>.Instance, services);
		var controller = new ActionController(fixture.Db, tasks);

		var result = Assert.IsType<AcceptedAtActionResult>(controller.Download(remote.Id, (string?)null));
		var id = Assert.IsType<Guid>(result.Value);
		await WaitForCompletion(tasks, id);
		Assert.Equal(new[] { "one" }, downloader.Ids);
	}

	[Fact]
	public async Task TaskServiceImmediatelyTracksTasksAndReportsFailures()
	{
		using var services = new ServiceCollection().BuildServiceProvider();
		var tasks = new TaskService(NullLogger<TaskService>.Instance, services);
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var id = tasks.StartTask(async (progress, _) =>
		{
			await gate.Task;
			progress.Report(TaskProgress.FromIndeterminate("Working"));
			throw new InvalidOperationException("Download failed");
		});
		Assert.NotNull(tasks.GetProgress(id));
		gate.SetResult();
		var result = await WaitForCompletion(tasks, id);
		Assert.True(result.IsError);
		Assert.Equal("Download failed", result.Status);
	}

	[Fact]
	public async Task InternetFetchAndDownloadStoreMediaInRepository()
	{
		using var fixture = new Fixture();
		var remote = fixture.AddRemote();
		var http = new HttpClient(new FakeHttpHandler(request => request.RequestUri!.AbsolutePath switch
		{
			"/playlist.m3u8" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("#EXTM3U\n#EXTINF:12,Song\ntracks/song.mp3\n") },
			"/tracks/song.mp3" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.UTF8.GetBytes("media")) },
			_ => new HttpResponseMessage(HttpStatusCode.NotFound)
		}));
		var service = new InternetRemoteService(http, fixture.Db, fixture.Repo);
		var progress = new RecordingProgress();

		await service.Fetch(remote, progress);
		var media = Assert.Single(fixture.Db.Medias);
		Assert.Equal(remote.Id, media.RemoteId);
		Assert.Equal("0", media.RemoteUID);
		await service.Download(remote, [media.RemoteUID!], progress);
		Assert.False(progress.Last!.IsError);
		Assert.Equal("media", await File.ReadAllTextAsync(Path.Combine(fixture.Repo.RootPath.FullName, media.FilePath!)));
	}

	[Fact]
	public async Task InternetSyncReportsMediaDownloadFailure()
	{
		using var fixture = new Fixture();
		var remote = fixture.AddRemote();
		var http = new HttpClient(new FakeHttpHandler(request => request.RequestUri!.AbsolutePath == "/playlist.m3u8"
			? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("#EXTM3U\n#EXTINF:12,Song\nmissing.mp3\n") }
			: new HttpResponseMessage(HttpStatusCode.NotFound)));
		var service = new InternetRemoteService(http, fixture.Db, fixture.Repo);
		var progress = new RecordingProgress();

		await service.Sync(remote, progress);

		Assert.True(progress.Last!.IsError);
		Assert.Contains("Song", progress.Last.Status);
		Assert.Contains("404", progress.Last.Status);
	}

	[Fact]
	public async Task InternetSyncDoesNotOverwriteMediaWithTheSameTitle()
	{
		using var fixture = new Fixture();
		var remote = fixture.AddRemote();
		using var http = new HttpClient(new FakeHttpHandler(request => request.RequestUri!.AbsolutePath switch
		{
			"/playlist.m3u8" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("#EXTM3U\n#EXTINF:1,Song\none.mp3\n#EXTINF:1,Song\ntwo.mp3\n") },
			"/one.mp3" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("first") },
			"/two.mp3" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("second") },
			_ => new HttpResponseMessage(HttpStatusCode.NotFound)
		}));
		var service = new InternetRemoteService(http, fixture.Db, fixture.Repo);
		var progress = new RecordingProgress();

		await service.Sync(remote, progress);

		Assert.False(progress.Last!.IsError, progress.Last.Status);
		var media = await fixture.Db.Medias.OrderBy(m => m.RemoteUID).ToArrayAsync();
		Assert.Equal(2, media.Length);
		Assert.NotEqual(media[0].FilePath, media[1].FilePath);
		Assert.Equal("first", await File.ReadAllTextAsync(Path.Combine(fixture.Repo.RootPath.FullName, media[0].FilePath!)));
		Assert.Equal("second", await File.ReadAllTextAsync(Path.Combine(fixture.Repo.RootPath.FullName, media[1].FilePath!)));
		await service.Sync(remote, progress);
		Assert.Equal(2, Directory.GetFiles(Path.Combine(fixture.Repo.RootPath.FullName, "media")).Length);
	}

	private static async Task<TaskProgress> WaitForCompletion(ITaskService tasks, Guid id)
	{
		for (var i = 0; i < 200; i++)
		{
			var result = tasks.GetProgress(id);
			if (result?.IsCompleted == true) return result;
			await Task.Delay(10);
		}
		throw new TimeoutException("Task did not complete.");
	}

	private sealed class Fixture : IDisposable
	{
		public TestRepo Repo { get; } = new();
		public PlayRepoDbContext Db { get; }

		public Fixture()
		{
			Db = new PlayRepoDbContext(Repo);
			Db.Database.EnsureCreated();
		}

		public RemotePlaylist AddRemote()
		{
			var remote = new RemotePlaylist { Link = "https://example.test/playlist.m3u8" };
			Db.RemotePlaylists.Add(remote);
			Db.SaveChanges();
			return remote;
		}

		public void Dispose()
		{
			var connection = (SqliteConnection)Db.Database.GetDbConnection();
			SqliteConnection.ClearPool(connection);
			Db.Dispose();
			Repo.RootPath.Delete(true);
		}
	}

	private sealed class TestRepo : IPlayRepoService
	{
		public DirectoryInfo RootPath { get; set; } = Directory.CreateTempSubdirectory();
		public DirectoryInfo? DotDir => RootPath.CreateSubdirectory(".playrepo");
		public bool IsRepoInitialized => true;
		public string GetRelativePath(string fullPath) => Path.GetRelativePath(RootPath.FullName, fullPath);
		public string GetRelativePath(FileInfo file) => GetRelativePath(file.FullName);
		public bool Initialize() => true;
	}

	private sealed class RecordingProgress : IProgress<TaskProgress>
	{
		public TaskProgress? Last { get; private set; }
		public void Report(TaskProgress value) => Last = value;
	}

	private sealed class FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
	}

	private sealed class RecordingRemoteService : IRemoteService
	{
		public string[] Ids { get; private set; } = [];
		public Task Download(RemotePlaylist remote, IEnumerable<string> mediaUIDs, IProgress<TaskProgress>? progress = null)
		{
			Ids = mediaUIDs.ToArray();
			progress?.Report(TaskProgress.FromCompleted());
			return Task.CompletedTask;
		}
		public Task Fetch(RemotePlaylist remote, IProgress<TaskProgress>? progress = null) => Task.CompletedTask;
		public Task Sync(RemotePlaylist remote, IProgress<TaskProgress>? progress = null) => Task.CompletedTask;
		public Task Update(IProgress<TaskProgress>? progress = null) => Task.CompletedTask;
	}
}
