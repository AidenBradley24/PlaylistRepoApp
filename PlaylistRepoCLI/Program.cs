using CommandLine;
//using LocalPlaylistMasterLib;

namespace LocalPlaylistMasterCLI;

public class Program
{
	[Verb("init", HelpText = "Initialize a new playlist repository.")]
	public class InitOptions
	{
		[Option("dir", Default = ".")]
		public string Path { get; set; } = ".";
	}

	public static int Main(string[] args)
	{
		return Parser.Default.ParseArguments<InitOptions>(args)
			.MapResult(
				opts => RunInit(opts),
				errs => 1
			);
	}

	public static int RunInit(InitOptions opts)
	{
		throw new NotImplementedException();
	}
}