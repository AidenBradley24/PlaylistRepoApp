using PlaylistRepoLib.Models;

namespace PlaylistRepoAPI
{
	public static class MediaExtensions
	{
		public static void SyncToMediaFile(this Media media)
		{
			if (media.FilePath == null)
				throw new InvalidOperationException(nameof(media.FilePath) + " must be specified to sync to file.");
			FileInfo file = new(media.FilePath);
			if (!file.Exists)
				throw new InvalidOperationException(nameof(media.FilePath) + " must exist to sync to file.");

			var tagFile = TagLib.File.Create(file.FullName);
			tagFile.Tag.Title = media.Title;
			tagFile.Tag.Album = media.Album ?? "";
			tagFile.Tag.Performers = media.Artists ?? [];
		}

		public static void SyncFromMediaFile(this Media media)
		{
			if (media.FilePath == null)
				throw new InvalidOperationException(nameof(media.FilePath) + " must be specified to sync from file.");
			FileInfo file = new(media.FilePath);
			if (!file.Exists)
				throw new InvalidOperationException(nameof(media.FilePath) + " must exist to sync from file.");

			if (media.Settings.HasFlag(MediaSettings.locked))
				return;

			var tagFile = TagLib.File.Create(file.FullName);
			media.Title = tagFile.Tag.Title;
			media.Album = tagFile.Tag.Album;
			media.Artists = tagFile.Tag.Performers;
		}
	}
}
