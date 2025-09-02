using PlaylistRepoLib.Models;

namespace PlaylistRepoAPI
{
	public static class RemotePlaylistExtensions
	{
		/// <summary>
		/// Get all media associated with this remote playlist
		/// </summary>
		public static IQueryable<Media> AllEntries(this RemotePlaylist remote, IQueryable<Media> library, bool requireFile = false)
		{
			var query = library.Where(m => m.RemoteId == remote.Id);
			if (requireFile) query = query.Where(media => media.FilePath != null);
			return query;
		}
	}
}
