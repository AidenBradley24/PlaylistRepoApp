using PlaylistRepoLib;
using System.Diagnostics;
using System.Net.Http.Json;

namespace PlaylistRepoCLI
{
	internal class ApiHandeler : IDisposable
	{
		const int STD_PORT = 7002;

		private readonly Process? apiProcess;
		public string ApiUrl { get; }

		private bool isDisposed = false;

		public const int POLLING_RATE = 500;

		/// <summary>
		/// Create a new API handeler for an existing API instance.
		/// </summary>
		/// <param name="apiUrl">The url to the API</param>
		public ApiHandeler(string apiUrl)
		{
			ApiUrl = apiUrl;
			if (ApiUrl.EndsWith('/')) ApiUrl = ApiUrl[..^1];
		}

		/// <summary>
		/// Start an API process and create a handeler for that API instance.
		/// </summary>
		/// <param name="repoDir">The repository root directory for the API</param>
		/// <param name="apiUrl">The URL to host the API through. Leave null to automatically pick.</param>
		public ApiHandeler(DirectoryInfo repoDir, string? apiUrl = null)
		{
			ApiUrl = apiUrl ??= $"http://localhost:{STD_PORT}";
			FileInfo process = new(Environment.ProcessPath!);
			ProcessStartInfo processStart = new(Path.Combine(process.DirectoryName!, "PlaylistRepoAPI.exe"), $"\"{repoDir.FullName}\" --ApiBaseUrl={apiUrl}")
			{
				RedirectStandardOutput = true,
			};
			apiProcess = Process.Start(processStart)!;
			if (ApiUrl.EndsWith('/')) ApiUrl = ApiUrl[..^1];
		}

		/// <summary>
		/// Request a task on the API be started an tracked
		/// </summary>
		/// <param name="requestUrl">Segment of url including leading forward slash</param>
		/// <param name="mutateHttpRequest">Use to modify the created HTTP request to include data.</param>
		public async Task<string?> TaskRequest(HttpMethod httpMethod, string requestUrl, Action<HttpRequestMessage>? mutateHttpRequest = null)
		{
			ObjectDisposedException.ThrowIf(isDisposed, this);

			using var http = new HttpClient();
			HttpRequestMessage httpRequest = new(httpMethod, ApiUrl + requestUrl);
			mutateHttpRequest?.Invoke(httpRequest);
			var response = await http.SendAsync(httpRequest);
			if (!response.IsSuccessStatusCode)
			{
				string? error = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"Error: {(int)response.StatusCode} {response.ReasonPhrase} {error}");
				return error;
			}
			string? id = await response.Content.ReadFromJsonAsync<string>();
			Guid guid = Guid.Parse(id!);
			Console.WriteLine("request started");

			TaskProgress? taskProgress = null;
			ConsoleHelpers.WriteProgress(taskProgress);
			do
			{
				ConsoleHelpers.RewriteProgress(taskProgress);
				await Task.Delay(POLLING_RATE);
				response = await http.GetAsync($"{ApiUrl}/api/service/status/{guid}");
				if (!response.IsSuccessStatusCode)
				{
					Console.WriteLine($"Error: {(int)response.StatusCode} {response.ReasonPhrase} {await response.Content.ReadAsStringAsync()}");
					break;
				}

				taskProgress = await response.Content.ReadFromJsonAsync<TaskProgress>();
			} while (!(taskProgress?.IsCompleted ?? false));
			ConsoleHelpers.ClearProgress();
			return taskProgress?.Status;
		}

		public async Task<(string? status, Stream? exportStream)> ExportRequest(HttpMethod httpMethod, string requestUrl, Action<HttpRequestMessage>? mutateHttpRequest = null)
		{
			ObjectDisposedException.ThrowIf(isDisposed, this);

			using var http = new HttpClient();
			HttpRequestMessage httpRequest = new(httpMethod, ApiUrl + requestUrl);
			mutateHttpRequest?.Invoke(httpRequest);
			var response = await http.SendAsync(httpRequest);
			if (!response.IsSuccessStatusCode)
			{
				string? error = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"Error: {(int)response.StatusCode} {response.ReasonPhrase} {error}");
				return (error, null);
			}
			string? id = await response.Content.ReadFromJsonAsync<string>();
			Guid guid = Guid.Parse(id!);
			Console.WriteLine("request started");

			TaskProgress? taskProgress = null;
			ConsoleHelpers.WriteProgress(taskProgress);
			do
			{
				ConsoleHelpers.RewriteProgress(taskProgress);
				await Task.Delay(POLLING_RATE);
				response = await http.GetAsync($"{ApiUrl}/api/service/status/{guid}");
				if (!response.IsSuccessStatusCode)
				{
					Console.WriteLine($"Error: {(int)response.StatusCode} {response.ReasonPhrase} {await response.Content.ReadAsStringAsync()}");
					break;
				}

				taskProgress = await response.Content.ReadFromJsonAsync<TaskProgress>();
			} while (!(taskProgress?.IsCompleted ?? false));
			ConsoleHelpers.ClearProgress();
			Console.WriteLine("Preparing export...");
			response = await http.GetAsync($"{ApiUrl}/api/export/result/{guid}");
			if (response.StatusCode != System.Net.HttpStatusCode.OK)
				return (taskProgress?.Status, null);
			return (taskProgress?.Status, response.Content.ReadAsStream());
		}

		/// <summary>
		/// Request a function on the API
		/// </summary>
		/// <param name="requestUrl">Segment of url including leading forward slash</param>
		/// <param name="mutateHttpRequest">Use to modify the created HTTP request to include data.</param>
		public async Task<HttpResponseMessage> Request(HttpMethod httpMethod, string requestUrl, Action<HttpRequestMessage>? mutateHttpRequest = null)
		{
			ObjectDisposedException.ThrowIf(isDisposed, this);

			using var http = new HttpClient();
			HttpRequestMessage httpRequest = new(httpMethod, ApiUrl + requestUrl);
			mutateHttpRequest?.Invoke(httpRequest);

			var response = await http.SendAsync(httpRequest);
			return response;
		}

		public void Dispose()
		{
			isDisposed = true;
			apiProcess?.Kill();
			apiProcess?.Dispose();
		}
	}
}
