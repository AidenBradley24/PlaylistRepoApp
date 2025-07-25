using PlaylistRepoLib;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;

namespace PlaylistRepoCLI
{
	internal class ApiHandeler : IDisposable
	{
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
		}

		/// <summary>
		/// Start an API process and create a handeler for that API instance.
		/// </summary>
		/// <param name="repoDir">The repository root directory for the API</param>
		/// <param name="apiUrl">The URL to host the API through. Leave null to automatically pick.</param>
		public ApiHandeler(DirectoryInfo repoDir, string? apiUrl = null)
		{
			ApiUrl = apiUrl ??= $"http://localhost:{GetFreePort()}";
			FileInfo process = new(Environment.ProcessPath!);
			ProcessStartInfo processStart = new(Path.Combine(process.DirectoryName!, "PlaylistRepoAPI.exe"), $"\"{repoDir.FullName}\" --ApiBaseUrl={apiUrl}")
			{
				RedirectStandardOutput = true,
			};
			apiProcess = Process.Start(processStart)!;
		}

		public static int GetFreePort()
		{
			TcpListener l = new(IPAddress.Loopback, 0);
			l.Start();
			int port = ((IPEndPoint)l.LocalEndpoint).Port;
			l.Stop();
			return port;
		}

		public async Task<string?> TaskRequest(HttpMethod method, string uri, HttpContent? content = null)
		{
			ObjectDisposedException.ThrowIf(isDisposed, this);

			using var http = new HttpClient();
			var httpRequest = new HttpRequestMessage(method, ApiUrl + uri)
			{
				Content = content
			};
			var response = await http.SendAsync(httpRequest);
			if (!response.IsSuccessStatusCode)
			{
				string? error = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"Error: {error}");
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
				response = await http.GetAsync($"{ApiUrl}/Task/status/{guid}");
				if (!response.IsSuccessStatusCode)
				{
					Console.WriteLine($"Error: {await response.Content.ReadAsStringAsync()}");
					break;
				}

				taskProgress = await response.Content.ReadFromJsonAsync<TaskProgress>();
			} while (!(taskProgress?.IsCompleted ?? false));
			ConsoleHelpers.ClearProgress();
			return taskProgress?.Status;
		}

		public async Task<HttpResponseMessage> Request(HttpMethod method, string uri, HttpContent? content = null)
		{
			ObjectDisposedException.ThrowIf(isDisposed, this);

			using var http = new HttpClient();
			var httpRequest = new HttpRequestMessage(method, ApiUrl + uri)
			{
				Content = content
			};
			var response = await http.SendAsync(httpRequest);
			if (!response.IsSuccessStatusCode)
			{
				Console.WriteLine($"Error: {await response.Content.ReadAsStringAsync()}");
			}
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
