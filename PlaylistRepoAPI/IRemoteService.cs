using PlaylistRepoLib;
using PlaylistRepoLib.Models;

namespace PlaylistRepoAPI
{
	public interface IRemoteService
	{
		/// <summary>
		/// Update the remote service to the latest version.
		/// </summary>
		public Task Update(IProgress<TaskProgress>? progress = null);

		/// <summary>
		/// Fetch and update media in database from <paramref name="remote"/> that is not <see cref="Media.Locked"/>.
		/// </summary>
		public Task Fetch(RemotePlaylist remote, IProgress<TaskProgress>? progress = null);

		/// <summary>
		/// Downloads and replaces <see cref="Media.File"/> from the requested <paramref name="remote"/> where the <see cref="Media.RemoteUID"/> is in <paramref name="mediaUIDs"/>.
		/// </summary>
		public Task Download(RemotePlaylist remote, IEnumerable<string> mediaUIDs, IProgress<TaskProgress>? progress = null);

		/// <summary>
		/// Fetch and download media from <paramref name="remote"/>.
		/// <br/> Ignores media that is <see cref="Media.Locked"/>.
		/// </summary>
		public Task Sync(RemotePlaylist remote, IProgress<TaskProgress>? progress = null);
	}
}
