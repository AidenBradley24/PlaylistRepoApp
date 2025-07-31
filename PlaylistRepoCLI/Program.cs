using CommandLine;
using System.Reflection;
using System.Text;

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
		var response = await api.Request(HttpMethod.Get, "/view/info");
		Console.WriteLine(await response.Content.ReadAsStringAsync());
		Console.WriteLine($"Created new playlist repository. Use ingest to add existing media files.");
		return 0;
	}

	[Verb("ingest", HelpText = "Add new media files within the repo directory to the database.")]
	class IngestOptions : ApiOptions
	{
		[Value(0, MetaName = "files", HelpText = "Search criteria for files to add", Required = true)] public string FileSpec { get; set; } = null!;
	}

	private static async Task<int> RunIngestAsync(IngestOptions opts)
	{
		using var api = opts.CreateAPI();
		HttpContent content = new StringContent($"\"{opts.FileSpec}\"", Encoding.UTF8, "application/json");
		var response = await api.TaskRequest(HttpMethod.Post, "/action/ingest", content);
		Console.WriteLine(response);
		return 0;
	}
}