using Microsoft.EntityFrameworkCore;
using PlaylistRepoLib.Models;
using PlaylistRepoLib.UserQueries;
using System.Text;
using System.Text.Encodings.Web;
using System.Xml;

namespace PlaylistRepoAPI
{
	public static class PlaylistExtensions
	{
		/// <summary>
		/// Get all media included in this playlist
		/// </summary>
		public static IQueryable<Media> AllEntries(this Playlist playlist, IQueryable<Media> library, bool requireFile = false)
		{
			var query = library.EvaluateUserQuery(playlist.UserQuery).Union(library.Where(media => playlist.BakedEntries.Contains(media.Id)));
			if (requireFile) query = query.Where(media => media.FilePath != null);
			return query;
		}

		/// <summary>
		/// Add all entries in <see cref="Playlist.UserQuery"/> to <see cref="Playlist.BakedEntries"/>
		/// </summary>
		public static void Bake(this Playlist playlist, IQueryable<Media> library)
		{
			foreach (var media in library.EvaluateUserQuery(playlist.UserQuery))
			{
				if (!playlist.BakedEntries.Contains(media.Id))
				{
					playlist.BakedEntries.Add(media.Id);
				}
			}
		}

		public static async Task StreamXspfAsync(this Playlist playlist, IQueryable<Media> library, Stream outputStream, PlaylistStreamingSettings settings)
		{
			XmlWriter writer = XmlWriter.Create(outputStream, new XmlWriterSettings() { Async = true });
			await writer.WriteStartDocumentAsync();
			await writer.WriteStartElementAsync(null, "playlist", "http://xspf.org/ns/0/");
			await writer.WriteAttributeStringAsync(null, "version", null, "1");
			await writer.WriteStartElementAsync(null, "trackList", null);

			var url = UrlEncoder.Default;
			var query = playlist.AllEntries(library, true);
			await query.LoadAsync();

			foreach (var media in query)
			{
				await writer.WriteStartElementAsync(null, "track", null);

				string location;
				if (settings.UseDirectory)
				{
					ArgumentNullException.ThrowIfNull(settings.RootDirectory, nameof(settings.RootDirectory));
					location = $"file:///{url.Encode(Path.GetRelativePath(settings.RootDirectory.FullName, media.FilePath!))}";
				}
				else
				{
					ArgumentNullException.ThrowIfNullOrWhiteSpace(settings.ApiUrl, nameof(settings.ApiUrl));
					location = $"{settings.ApiUrl}/play/media/{media.Id}";
				}
				await writer.WriteElementStringAsync(null, "location", null, location);

				await writer.WriteElementStringAsync(null, "title", null, media.Title);
				if (media.PrimaryArtist != null) await writer.WriteElementStringAsync(null, "creator", null, media.PrimaryArtist);
				if (media.Album != null || settings.OverrideAlbum != null)
					await writer.WriteElementStringAsync(null, "album", null, settings.OverrideAlbum ?? media.Album!);
				if (media.Length > TimeSpan.Zero) await writer.WriteElementStringAsync(null, "duration", null, media.Length.Milliseconds.ToString());

				await writer.WriteEndElementAsync();
			}

			await writer.WriteEndDocumentAsync();
			await writer.FlushAsync();
		}

		public static async Task StreamM3U8Async(this Playlist playlist, IQueryable<Media> library, Stream outputStream, PlaylistStreamingSettings settings)
		{
			var writer = new StreamWriter(outputStream, Encoding.UTF8);
			await writer.WriteLineAsync("#EXTM3U");

			var url = UrlEncoder.Default;
			var query = playlist.AllEntries(library, true);
			await query.LoadAsync();

			foreach (var media in query)
			{
				string location;
				if (settings.UseDirectory)
				{
					ArgumentNullException.ThrowIfNull(settings.RootDirectory, nameof(settings.RootDirectory));
					location = $"file:///{url.Encode(Path.GetRelativePath(settings.RootDirectory.FullName, media.FilePath!))}";
				}
				else
				{
					ArgumentNullException.ThrowIfNullOrWhiteSpace(settings.ApiUrl, nameof(settings.ApiUrl));
					location = $"https://{settings.ApiUrl}/play/media/{media.Id}";
				}

				string title = media.Title.Replace(",", "").Replace("-", "|").Trim();

				await writer.WriteAsync("#EXTINF:");
				await writer.WriteAsync((media.Length.Seconds).ToString());
				await writer.WriteAsync(',');

				if (media.PrimaryArtist != null)
				{
					await writer.WriteAsync(media.PrimaryArtist);
					await writer.WriteAsync(" - ");
				}

				await writer.WriteLineAsync(title);
				await writer.WriteLineAsync(location);
			}

			await writer.FlushAsync();
		}
	}

	public class PlaylistStreamingSettings
	{
		/// <summary>
		/// True if using a file directory instead of streaming from the server
		/// </summary>
		public bool UseDirectory { get; set; } = false;

		/// <summary>
		/// The root directory of media used when <see cref="UseDirectory"/> is true
		/// </summary>
		public DirectoryInfo? RootDirectory { get; set; }

		/// <summary>
		/// The api url used when <see cref="UseDirectory"/> is false (no trailing '/')
		/// </summary>
		public string? ApiUrl { get; set; }

		/// <summary>
		/// If not null, override the medias' album with this string
		/// </summary>
		public string? OverrideAlbum { get; set; }
	}
}
