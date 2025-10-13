using PlaylistRepoLib;
using PlaylistRepoLib.Models;

namespace PlaylistRepoAPI
{
	public static class MediaExtensions
	{
		public static void SyncToMediaFile(this Media media)
		{
			try
			{
				using var tagFile = media.GetTagFile();
				tagFile.Tag.Title = media.Title;
				tagFile.Tag.Album = media.Album ?? "";
				tagFile.Tag.Performers = media.Artists ?? [];
				tagFile.Tag.Genres = [media.Genre];
				tagFile.Save();
			}
			catch
			{
				Console.WriteLine("Error writing tags");
				// do nothing
			}	
		}

		public static void SyncFromMediaFile(this Media media)
		{
			if (media.Locked)
				return;

			media.MimeType = MimeTypes.GetMimeType(media.File!.Name);

			try
			{
				using var tagFile = media.GetTagFile();
				if (tagFile.Tag.Title != null) media.Title = tagFile.Tag.Title;
				if (tagFile.Tag.Album != null) media.Album = tagFile.Tag.Album;
				if (tagFile.Tag.Performers != null) media.Artists = tagFile.Tag.Performers;
				if (tagFile.Tag.Genres.Length > 0) media.Genre = tagFile.Tag.Genres[0];
				media.LengthMilliseconds = tagFile.Properties.Duration.Ticks / TimeSpan.TicksPerMillisecond;
			}
			catch
			{
				// do nothing
			}
		}

		/// <summary>
		/// Get the <see cref="TagLib.File"/> for this media instance.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="TagLib.UnsupportedFormatException"></exception>
		public static TagLib.File GetTagFile(this Media media)
		{
			if (media.FilePath == null)
				throw new InvalidOperationException(nameof(media.FilePath) + " must be specified to utilize tags.");
			FileInfo file = new(media.FilePath);
			if (!file.Exists)
				throw new InvalidOperationException(nameof(media.FilePath) + " must exist to utilize tags.");
			return TagLib.File.Create(file.FullName);
		}
	}
}
