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

		private static string GetLocation(Media media, PlaylistStreamingSettings settings)
		{
			if (settings.UseDirectory)
			{
				if (settings.RootDirectory != null)
				{
					return $"file:///{UrlEncoder.Default.Encode(Path.GetRelativePath(settings.RootDirectory.FullName, media.FilePath!))}";
				}
				else if (settings.MediaPath != null)
				{
					return $"file:///{UrlEncoder.Default.Encode(Path.Combine(settings.MediaPath, media.File!.Name))}";
				}
				else
				{
					throw new Exception("Root Directory and Media Path can't both be null.");
				}
			}
			else
			{
				return $"{settings.ApiUrl}/api/play/media/{media.Id}";
			}
		}

		/// <summary>
		/// Get this playlist as a XSPF playlist. <a href="https://www.xspf.org/"/> 
		/// </summary>
		/// <param name="playlist">The playlist to convert</param>
		/// <param name="library">The collection of all media</param>
		/// <param name="outputStream">Stream to export to</param>
		/// <param name="settings">Additional export settings</param>
		public static async Task StreamXspfAsync(this Playlist playlist, IQueryable<Media> library, Stream outputStream, PlaylistStreamingSettings settings)
		{
			XmlWriter writer = XmlWriter.Create(outputStream, new XmlWriterSettings() { Async = true });
			await writer.WriteStartDocumentAsync();
			await writer.WriteStartElementAsync(null, "playlist", "http://xspf.org/ns/0/");
			await writer.WriteAttributeStringAsync(null, "version", null, "1");
			await writer.WriteStartElementAsync(null, "trackList", null);

			var query = playlist.AllEntries(library, true);
			await query.LoadAsync();

			foreach (var media in query)
			{
				await writer.WriteStartElementAsync(null, "track", null);

				string location = GetLocation(media, settings);
				await writer.WriteElementStringAsync(null, "location", null, location);

				await writer.WriteElementStringAsync(null, "title", null, media.Title);
				if (media.PrimaryArtist != null) await writer.WriteElementStringAsync(null, "creator", null, media.PrimaryArtist);
				if (media.Album != null || settings.OverrideAlbum != null)
					await writer.WriteElementStringAsync(null, "album", null, settings.OverrideAlbum ?? media.Album!);
				if (media.LengthMilliseconds > 0) await writer.WriteElementStringAsync(null, "duration", null, media.LengthMilliseconds.ToString());

				await writer.WriteEndElementAsync();
			}

			await writer.WriteEndDocumentAsync();
			await writer.FlushAsync();
		}

		/// <summary>
		/// Get this playlist as a M3U8 playlist.
		/// </summary>
		/// <param name="playlist">The playlist to convert</param>
		/// <param name="library">The collection of all media</param>
		/// <param name="outputStream">Stream to export to</param>
		/// <param name="settings">Additional export settings</param>
		public static async Task StreamM3U8Async(this Playlist playlist, IQueryable<Media> library, Stream outputStream, PlaylistStreamingSettings settings)
		{
			var writer = new StreamWriter(outputStream, Encoding.UTF8);
			await writer.WriteLineAsync("#EXTM3U");

			var query = playlist.AllEntries(library, true);
			await query.LoadAsync();

			foreach (var media in query)
			{
				string location = GetLocation(media, settings);
				string title = media.Title.Replace(",", "").Replace("-", "|").Trim();

				await writer.WriteAsync("#EXTINF:");
				await writer.WriteAsync((media.LengthMilliseconds / 1000).ToString());
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

		/// <summary>
		/// Get this playlist as a CSV.
		/// </summary>
		/// <param name="playlist">The playlist to convert</param>
		/// <param name="library">The collection of all media</param>
		/// <param name="outputStream">Stream to export to</param>
		/// <param name="settings">Additional export settings</param>
		public static async Task StreamCSVAsync(this Playlist playlist, IQueryable<Media> library, Stream outputStream, PlaylistStreamingSettings settings, char delimiter)
		{
			var writer = new StreamWriter(outputStream, Encoding.UTF8);
			// id, title, ruid, path, hash, mime, primary artist, album, description, rating, length, order

			void Append(object? value)
			{
				string text = value?.ToString() ?? "";
				for (var i = 0; i < text.Length; i++)
				{
					if (text[i] == delimiter) continue;
					if (char.IsWhiteSpace(text[i]))
						writer.Write(' ');
					else
						writer.Write(text[i]);
				}
				writer.Write(delimiter);
			}

			Append("id"); 
			Append("title");
			Append("ruid");
			Append("location");
			Append("hash");
			Append("mime");
			Append("primary artist");
			Append("album");
			Append("description");
			Append("rating");
			Append("length milliseconds");
			Append("order");

			var query = playlist.AllEntries(library, true);
			await query.LoadAsync();

			foreach (var media in query)
			{
				writer.WriteLine();
				string location = GetLocation(media, settings);
				string hash = media.Hash != null ? Convert.ToBase64String(media.Hash) : "";
				Append(media.Id);
				Append(media.Title);
				Append(media.RemoteUID);
				Append(location);
				Append(hash);
				Append(media.MimeType);
				Append(media.PrimaryArtist);
				Append(media.Album);
				Append(media.Description);
				Append(media.Rating);
				Append(media.LengthMilliseconds);
				Append(media.Order);
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
		/// The root directory of media used when <see cref="UseDirectory"/> is true <br/>
		/// <see cref="RootDirectory"/> and <see cref="MediaPath"/> are mutually exclusive.
		/// </summary>
		public DirectoryInfo? RootDirectory { get; set; }

		/// <summary>
		/// The override file path used when <see cref="UseDirectory"/> is true <br/>
		/// <see cref="RootDirectory"/> and <see cref="MediaPath"/> are mutually exclusive.
		/// </summary>
		public string? MediaPath { get; set; }

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
