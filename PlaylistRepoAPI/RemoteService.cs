using PlaylistRepoLib;
using PlaylistRepoLib.Models;

namespace PlaylistRepoAPI
{
	public class RemoteService(IServiceProvider serviceProvider) : IRemoteService
	{
		public static IRemoteService DetermineService(IServiceProvider services, RemotePlaylist remote)
		{
			IRemoteService? service = remote.Type switch
			{
				RemotePlaylist.RemoteType.internet => services.GetService<InternetRemoteService>(),
				RemotePlaylist.RemoteType.ytdlp => services.GetService<YtDlpService>(),
				_ => throw new NotImplementedException()
			};

			return service ?? throw new Exception("Service was not found.");
		}

		public async Task Download(RemotePlaylist remote, IEnumerable<string> mediaUIDs, IProgress<TaskProgress>? progress = null)
		{
			using var scope = serviceProvider.CreateScope();
			var service = DetermineService(scope.ServiceProvider, remote);
			await service.Download(remote, mediaUIDs, progress);
		}

		public async Task Fetch(RemotePlaylist remote, IProgress<TaskProgress>? progress = null)
		{
			using var scope = serviceProvider.CreateScope();
			var service = DetermineService(scope.ServiceProvider, remote);
			await service.Fetch(remote, progress);
		}

		public async Task Sync(RemotePlaylist remote, IProgress<TaskProgress>? progress = null)
		{
			using var scope = serviceProvider.CreateScope();
			var service = DetermineService(scope.ServiceProvider, remote);
			await service.Sync(remote, progress);
		}

		public async Task Update(IProgress<TaskProgress>? progress = null)
		{
			throw new NotImplementedException();
			// TODO update all services
		}
	}
}
