using CommandLine;
using PlaylistRepoLib.Models;
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
				(InitOptions opts) => RunInitAsync(opts),
				(IngestOptions opts) => RunIngestAsync(opts),
				(AddOptions opts) => RunAddAsync(opts),
				(FetchOptions opts) => RunFetchAsync(opts),
				(CreateOptions opts) => RunCreateAsync(opts),
				(ListOptions opts) => RunListAsync(opts),
				errs => Task.FromResult(-1)
			);
	}

	abstract class ApiOptions
	{
		[Option(MetaValue = "http://url-to-api", Default = null, HelpText = "Specify a URL to a running API.", Required = false)] public string? ApiUrl { get; set; }

		public ApiHandeler CreateAPI()
		{
			return ApiUrl == null ?
				new ApiHandeler(new DirectoryInfo(Environment.CurrentDirectory)) :
				new ApiHandeler(ApiUrl);
		}
	}

	[Verb("init", HelpText = "Initialize a new playlist repository.")]
	class InitOptions : ApiOptions
	{

	}

	private static async Task<int> RunInitAsync(InitOptions opts)
	{
		using var api = opts.CreateAPI();
		var response = await api.Request(HttpMethod.Get, "/data/info");
		Console.WriteLine(await response.Content.ReadAsStringAsync());
		Console.WriteLine($"Created new playlist repository. Use ingest to add existing media files.");
		return 0;
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

	[Verb("fetch", HelpText = "Fetch media metadata from a remote YT playlist.")]
	class FetchOptions : ApiOptions
	{
		[Value(0, MetaName = "id", HelpText = "Numeric ID or the name of the remote playlist", Required = true)] public string RemoteId { get; set; } = null!;
	}

	private static async Task<int> RunFetchAsync(FetchOptions opts)
	{
		using var api = opts.CreateAPI();
		var response = await api.TaskRequest(HttpMethod.Post, "/action/fetch", request =>
		{
			request.Headers.Add("remoteId", opts.RemoteId);
		});
		Console.WriteLine(response);
		return 0;
	}

	[Verb("add", HelpText = "Add a new remote playlist to this repo.")]
	class AddOptions : ApiOptions
	{
		[Value(0, MetaName = "url", HelpText = "URL of the specified remote playlist.", Required = true)] public string RemoteURL { get; set; } = null!;
		[Value(1, MetaName = "name", HelpText = "Name of the remote playlist", Required = false)] public string? RemoteName { get; set; } = null;
		[Value(2, MetaName = "description", HelpText = "Description of the remote playlist", Required = false)] public string? RemoteDescription { get; set; } = null;
	}

	private static async Task<int> RunAddAsync(AddOptions opts)
	{
		using var api = opts.CreateAPI();
		var response = await api.Request(HttpMethod.Post, "/data/remotes", request =>
		{
			RemotePlaylist newRemote = new() { Link = opts.RemoteURL };
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
		var response = await api.Request(HttpMethod.Post, "/data/playlists", request =>
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

		[Option("pagenum", Default = 0, Required = false)]
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

		if (opts.ListMedia)
		{
			var response = await api.Request(HttpMethod.Get, $"/data/media?query={userQuery}&pageSize={opts.PageSize}&currentPage={opts.PageNumber}");
			result.AppendLine("\nMEDIA:");
			result.AppendJoin("\n", response.Content.ReadFromJsonAsAsyncEnumerable<Media>().ToBlockingEnumerable());
		}
		else if (opts.ListRemotePlaylists)
		{
			var response = await api.Request(HttpMethod.Get, $"/data/remotes?query={userQuery}&pageSize={opts.PageSize}&currentPage={opts.PageNumber}");
			result.AppendLine("\nREMOTE PLAYLISTS:");
			result.AppendJoin("\n", response.Content.ReadFromJsonAsAsyncEnumerable<RemotePlaylist>().ToBlockingEnumerable());
		}
		else if (opts.ListPlaylists)
		{
			var response = await api.Request(HttpMethod.Get, $"/data/playlists?query={userQuery}&pageSize={opts.PageSize}&currentPage={opts.PageNumber}");
			result.AppendLine("\nPLAYLISTS:");
			result.AppendJoin("\n", response.Content.ReadFromJsonAsAsyncEnumerable<Playlist>().ToBlockingEnumerable());
		}

		Console.WriteLine(result.ToString());
		return 0;
	}
}