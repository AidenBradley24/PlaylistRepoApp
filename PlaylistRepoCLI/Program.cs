using CommandLine;
using PlaylistRepoLib.Models;
using PlaylistRepoLib.Models.DTOs;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Web;

namespace PlaylistRepoCLI;

public class Program
{
	static async Task<int> Main(string[] args)
	{
		Type[] optionTypes = [.. typeof(Program).GetNestedTypes(BindingFlags.NonPublic).Where(t => t.GetCustomAttribute<VerbAttribute>() != null)];
		return await Parser.Default.ParseArguments(args, optionTypes)
			.MapResult(
				(HostOptions opts) => RunHostAsync(opts),
				(InitOptions opts) => RunInitAsync(opts),
				(IngestOptions opts) => RunIngestAsync(opts),
				(AddOptions opts) => RunAddAsync(opts),
				(SyncOptions opts) => RunSyncAsync(opts),
				(FetchOptions opts) => RunFetchAsync(opts),
				(CreateOptions opts) => RunCreateAsync(opts),
				(ListOptions opts) => RunListAsync(opts),
				(ExportOptions opts) => RunExportAsync(opts),
				errs => Task.FromResult(-1)
			);
	}

	abstract class ApiOptions
	{
		[Option('d', "dir", HelpText = "Specify a directory to serve. Defaults to current directory. Ignored if the url is set.", Required = false)]
		public string? RepoDirectory { get; set; }
		[Option('l', "url", MetaValue = "http://url-to-api", Default = null, HelpText = "Specify a URL to a running API.", Required = false)] 
		public string? ApiUrl { get; set; }
		public ApiHandeler CreateAPI()
		{
			if (ApiUrl != null)
				return new ApiHandeler(ApiUrl);
			string dir = RepoDirectory ?? Environment.CurrentDirectory;
			return new ApiHandeler(new DirectoryInfo(dir));	
		}
	}

	[Verb("host", aliases: ["start", "serve"], HelpText = "Start the server")]
	class HostOptions
	{
		[Option('d', "dir", HelpText = "Specify a directory to run the serve on. Defaults to current directory.", Required = false)]
		public string? PathToHost { get; set; }
	}

	private static Task<int> RunHostAsync(HostOptions opts)
	{
		var dir = new DirectoryInfo(opts.PathToHost ?? Environment.CurrentDirectory);
		using var api = new ApiHandeler(dir);
		Console.WriteLine($"Started hosting playlist repository at '{dir.FullName}'");
		Console.WriteLine(api.ApiUrl);
		bool exit = false;
		Console.CancelKeyPress += (_, _) => exit = true;
		while (!exit)
		{
			Console.Write(">> ");
			Console.ReadLine();
		}
		Console.WriteLine("Host Terminated");
		return Task.FromResult(0);
	}

	[Verb("init", HelpText = "Initialize a new playlist repository.")]
	class InitOptions : ApiOptions
	{

	}

	private static async Task<int> RunInitAsync(InitOptions opts)
	{
		using var api = opts.CreateAPI();
		var response = await api.Request(HttpMethod.Post, "/api/service/init");
		if (response.IsSuccessStatusCode)
		{
			Console.WriteLine($"Repo is now initialized.");
			return 0;
		}
		else
		{
			Console.WriteLine("Unable to initialize. Either the repo is already initialized or there is a file system issue.");
			Console.WriteLine((int)response.StatusCode);
			Console.WriteLine(response.ReasonPhrase);
			return 1;
		}
	}

	[Verb("ingest", HelpText = "Add new media files within the repo directory to the database.")]
	class IngestOptions : ApiOptions
	{
		[Value(0, MetaName = "files", HelpText = "Search criteria for files to add", Required = false)] public string FileSpec { get; set; } = "*";
	}

	private static async Task<int> RunIngestAsync(IngestOptions opts)
	{
		using var api = opts.CreateAPI();
		var response = await api.TaskRequest(HttpMethod.Post, "/action/ingest", request =>
		{
			request.Content = new StringContent($"\"{opts.FileSpec}\"", Encoding.UTF8, "application/json");
		});
		Console.WriteLine(response);
		return 0;
	}

	[Verb("fetch", HelpText = "Fetch media metadata from a remote playlist.")]
	class FetchOptions : ApiOptions
	{
		[Value(0, MetaName = "id", HelpText = "Numeric ID or the name of the remote playlist", Required = true)] public string RemoteId { get; set; } = null!;
	}

	private static async Task<int> RunFetchAsync(FetchOptions opts)
	{
		using var api = opts.CreateAPI();
		var response = await api.TaskRequest(HttpMethod.Post, "/api/action/fetch", request =>
		{
			request.Headers.Add("remoteId", opts.RemoteId);
		});
		Console.WriteLine(response);
		return 0;
	}

	[Verb("sync", HelpText = "Sync media metadata and download from a remote playlist.")]
	class SyncOptions : ApiOptions
	{
		[Value(0, MetaName = "id", HelpText = "Numeric ID or the name of the remote playlist", Required = true)] public string RemoteId { get; set; } = null!;
	}

	private static async Task<int> RunSyncAsync(SyncOptions opts)
	{
		using var api = opts.CreateAPI();
		var response = await api.TaskRequest(HttpMethod.Post, "/api/action/sync", request =>
		{
			request.Headers.Add("remoteId", opts.RemoteId);
		});
		Console.WriteLine(response);
		return 0;
	}

	[Verb("add", HelpText = "Add a new remote playlist to this repo.")]
	class AddOptions : ApiOptions
	{
		[Value(0, MetaName = "type", HelpText = "internet or ytdlp", Required = true)] public string Type { get; set; } = null!;
		[Value(1, MetaName = "url", HelpText = "URL of the specified remote playlist.", Required = true)] public string RemoteURL { get; set; } = null!;
		[Option('m', "media-type", HelpText = "MIME type of the media in the playlist", Required = false, Default = null)] public string? MediaType { get; set; } = null;
		[Option('n', "name", HelpText = "Name of the remote playlist", Required = false)] public string? RemoteName { get; set; } = null;
		[Option('d', "description", HelpText = "Description of the remote playlist", Required = false)] public string? RemoteDescription { get; set; } = null;
	}

	private static async Task<int> RunAddAsync(AddOptions opts)
	{
		using var api = opts.CreateAPI();
		var response = await api.Request(HttpMethod.Post, "/api/data/remotes", request =>
		{
			RemotePlaylist newRemote = new()
			{
				Type = Enum.Parse<RemotePlaylist.RemoteType>(opts.Type),
				Link = opts.RemoteURL,
				MediaMime = opts.MediaType
			};
			if (opts.RemoteName != null) newRemote.Name = opts.RemoteName;
			if (opts.RemoteDescription != null) newRemote.Description = opts.RemoteDescription;
			request.Content = JsonContent.Create(newRemote);
		});
		Console.WriteLine(response);
		return 0;
	}

	[Verb("create", HelpText = "Create a new playlist from media on this repo.")]
	class CreateOptions : ApiOptions
	{
		[Value(0, MetaName = "contents", HelpText = "User Query to specify the contents of this playlist", Required = true)] public string UserQuery { get; set; } = null!;
		[Value(1, MetaName = "title", HelpText = "Title of the playlist", Required = false)] public string? PlaylistTitle { get; set; } = null;
		[Value(2, MetaName = "description", HelpText = "Description of the playlist", Required = false)] public string? PlaylistDescription { get; set; } = null;
	}

	private static async Task<int> RunCreateAsync(CreateOptions opts)
	{
		using var api = opts.CreateAPI();
		var response = await api.Request(HttpMethod.Post, "/api/data/playlists", request =>
		{
			Playlist newPlaylist = new() { UserQuery = opts.UserQuery };
			if (opts.PlaylistTitle != null) newPlaylist.Title = opts.PlaylistTitle;
			if (opts.PlaylistDescription != null) newPlaylist.Description = opts.PlaylistDescription;
			request.Content = JsonContent.Create(newPlaylist);
		});
		Console.WriteLine(response);
		return 0;
	}

	[Verb("list", HelpText = "List contents of the database")]
	class ListOptions : ApiOptions
	{
		[Value(0, MetaName = "User query", Default = "", Required = false, HelpText = "A filter upon the output returned.")]
		public string UserQuery { get; set; } = "";

		[Option('m', "media", Required = false, HelpText = "list media")]
		public bool ListMedia { get; set; }

		[Option('r', "remote-playlists", Required = false, HelpText = "list remote playlists")]
		public bool ListRemotePlaylists { get; set; }

		[Option('p', "playlists", Required = false, HelpText = "list playlists")]
		public bool ListPlaylists { get; set; }

		[Option("pagesize", Default = 50, Required = false)]
		public int PageSize { get; set; }

		[Option("pagenum", Default = 1, Required = false)]
		public int PageNumber { get; set; }
	}

	private static async Task<int> RunListAsync(ListOptions opts)
	{
		using var api = opts.CreateAPI();

		StringBuilder result = new();
		result.Append("Page ");
		result.Append(opts.PageNumber);
		result.Append(" listing ");
		result.Append(opts.PageSize);
		result.AppendLine(" entries per page.");
		string userQuery = HttpUtility.UrlEncode(opts.UserQuery);

		if (!opts.ListMedia && !opts.ListRemotePlaylists & !opts.ListPlaylists)
		{
			Console.WriteLine("Add the -m or -r or -p flag to specify what to list.");
			return 1;
		}

		async Task list<TModel, TDTO>(string url, string label) where TModel : class, new() where TDTO : DataTransferObject<TModel>, new()
		{
			var response = await api.Request(HttpMethod.Get, $"/api/data/media?query={userQuery}&pageSize={opts.PageSize}&currentPage={opts.PageNumber}");
			if (!response.IsSuccessStatusCode)
			{
				result.Append("Error fetching data: ");
				result.AppendLine(await response.Content.ReadAsStringAsync());
				return;
			}

			var formattedResponse = await response.Content.ReadFromJsonAsync<ApiGetResponse<TModel, TDTO>>();
			if (formattedResponse == null)
			{
				result.AppendLine("Error reading data.");
			}
			else
			{
				result.AppendLine();
				result.Append(formattedResponse.Total);
				result.Append(' ');
				result.Append(label);
				result.Append(':');
				result.AppendJoin<TDTO>("\n", formattedResponse.Data ?? []);
			}
		}

		if (opts.ListMedia)
			await list<Media, MediaDTO>($"/api/data/media?query={userQuery}&pageSize={opts.PageSize}&currentPage={opts.PageNumber}", "MEDIA");

		if (opts.ListRemotePlaylists)
			await list<RemotePlaylist, RemotePlaylistDTO>($"/api/data/remotes?query={userQuery}&pageSize={opts.PageSize}&currentPage={opts.PageNumber}", "REMOTE PLAYLISTS");

		if (opts.ListPlaylists)
			await list<Playlist, PlaylistDTO>($"/api/data/playlists?query={userQuery}&pageSize={opts.PageSize}&currentPage={opts.PageNumber}", "PLAYLISTS");

		Console.WriteLine(result.ToString());
		return 0;
	}

	[Verb("export", HelpText = "Export a particular playlist in a specific format: .xspf .m3u8 .zip .csv")]
	class ExportOptions : ApiOptions
	{
		[Value(0, MetaName = "Playlist ID", Required = true, HelpText = "A filter upon the output returned.")]
		public int PlaylistId { get; set; }

		[Value(0, MetaName = "Export Path", Required = true, HelpText = "Full path of exported file.")]
		public string ExportPath { get; set; } = "";
	}

	private static async Task<int> RunExportAsync(ExportOptions opts)
	{
		using var api = opts.CreateAPI();
		string fileExtenstion = Path.GetExtension(opts.ExportPath);
		var (response, stream) = await api.ExportRequest(HttpMethod.Get, $"/api/export/playlist/{opts.PlaylistId}{fileExtenstion}");
		Console.WriteLine(response);
		if (stream == null)
		{
			Console.WriteLine("No content to export.");
			return 1;
		}
		try
		{
			using FileStream fs = new(opts.ExportPath, FileMode.CreateNew, FileAccess.Write);
			await stream.CopyToAsync(fs);
		}
		catch (Exception ex)
		{
			Console.WriteLine("Unable to export file.");
			Console.WriteLine(ex.Message);
		}
		finally
		{
			stream.Dispose();
		}
		return 0;
	}
}