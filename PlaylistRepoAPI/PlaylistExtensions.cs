using Microsoft.EntityFrameworkCore;
using PlaylistRepoLib;
using PlaylistRepoLib.Models;
using System.Text;
using System.Text.Encodings.Web;
using System.Xml;
using System.Xml.Linq;
using UserQueries;

namespace PlaylistRepoAPI
{
	public static class PlaylistExtensions
	{
		/// <summary>
		/// Get all media included in this playlist
		/// </summary>
		public static IQueryable<Media> AllEntries(this Playlist playlist, IQueryable<Media> library, bool requireFile = false)
		{
			IQueryable<Media> query = playlist.UserQuery != "/" ? library.EvaluateUserQuery(playlist.UserQuery) : Enumerable.Empty<Media>().AsQueryable();
			query = query.Union(playlist.WhiteList.Select(id => library.First(m => m.Id == id)));
			query = query.ExceptBy(playlist.BlackList, m => m.Id);
			if (requireFile) query = query.Where(media => media.FilePath != null);
			return query;
		}

		/// <summary>
		/// Add all entries in <see cref="Playlist.UserQuery"/> to <see cref="Playlist.WhiteList"/>
		/// </summary>
		public static void Bake(this Playlist playlist, IQueryable<Media> library)
		{
			var query = library.EvaluateUserQuery(playlist.UserQuery);
			HashSet<int> entries = [.. playlist.WhiteList, .. query.Select(m => m.Id)];
			foreach (int id in playlist.BlackList)
			{
				entries.Remove(id);
			}

			playlist.WhiteList = [.. entries];
			playlist.BlackList = [];
			playlist.UserQuery = "/";
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

		#region Streaming Playlists

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

		#endregion

		#region Playlist Parsers

		public static async Task<(Playlist playlist, List<Media> mediaList)> ParseXspfAsync(Stream inputStream)
		{
			var mediaList = new List<Media>();
			var bakedEntries = new List<int>();
			var doc = await XDocument.LoadAsync(inputStream, LoadOptions.None, default);

			var playlistElem = doc.Root;
			var title = playlistElem?.Element(XName.Get("title", "http://xspf.org/ns/0/"))?.Value ?? "Imported XSPF";
			var description = playlistElem?.Element(XName.Get("annotation", "http://xspf.org/ns/0/"))?.Value ?? "";

			var trackList = playlistElem?.Element(XName.Get("trackList", "http://xspf.org/ns/0/"));
			if (trackList != null)
			{
				foreach (var track in trackList.Elements(XName.Get("track", "http://xspf.org/ns/0/")))
				{
					var media = new Media
					{
						Title = track.Element(XName.Get("title", "http://xspf.org/ns/0/"))?.Value ?? "",
						PrimaryArtist = track.Element(XName.Get("creator", "http://xspf.org/ns/0/"))?.Value ?? "",
						Album = track.Element(XName.Get("album", "http://xspf.org/ns/0/"))?.Value ?? "",
						LengthMilliseconds = long.TryParse(track.Element(XName.Get("duration", "http://xspf.org/ns/0/"))?.Value, out var len) ? len : 0,
						FilePath = track.Element(XName.Get("location", "http://xspf.org/ns/0/"))?.Value ?? "",
						Description = "",
						MimeType = MimeTypes.GetMimeType(Path.GetExtension(track.Element(XName.Get("location", "http://xspf.org/ns/0/"))?.Value ?? "")),
						Rating = 0,
						Order = mediaList.Count,
						RemoteUID = mediaList.Count.ToString(), // NOTE that this means if the order of the playlist changes the playlist can't keep track of ids
					};
					mediaList.Add(media);
				}
			}

			var playlist = new Playlist
			{
				Title = title,
				Description = description,
				UserQuery = "",
				WhiteList = bakedEntries
			};

			return (playlist, mediaList);
		}

		public static async Task<(Playlist playlist, List<Media> mediaList)> ParseM3U8Async(Stream inputStream)
		{
			var mediaList = new List<Media>();
			var bakedEntries = new List<int>();
			using var reader = new StreamReader(inputStream, Encoding.UTF8);
			string? line;
			string title = "Imported M3U8";
			string description = "";
			while ((line = await reader.ReadLineAsync()) != null)
			{
				if (line.StartsWith("#EXTINF:"))
				{
					var extinf = line[8..];
					var commaIdx = extinf.IndexOf(',');
					var duration = commaIdx > 0 ? extinf[..commaIdx] : "0";
					var info = commaIdx > 0 ? extinf[(commaIdx + 1)..] : "";
					var nextLine = await reader.ReadLineAsync();
					if (nextLine != null && !nextLine.StartsWith("#"))
					{
						var media = new Media
						{
							Title = info,
							LengthMilliseconds = long.TryParse(duration, out var len) ? len * 1000 : 0,
							FilePath = nextLine,
							Description = "",
							MimeType = "",
							Rating = 0,
							Order = mediaList.Count,
							RemoteUID = mediaList.Count.ToString(), // NOTE that this means if the order of the playlist changes the playlist can't keep track of ids
						};
						mediaList.Add(media);
						bakedEntries.Add(media.Id);
					}
				}
			}
			var playlist = new Playlist
			{
				Title = title,
				Description = description,
				UserQuery = "",
				WhiteList = bakedEntries
			};
			return (playlist, mediaList);
		}

		public static async Task<(Playlist playlist, List<Media> mediaList)> ParseCSVAsync(Stream inputStream, char delimiter)
		{
			var mediaList = new List<Media>();
			var bakedEntries = new List<int>();
			using var reader = new StreamReader(inputStream, Encoding.UTF8);
			string? headerLine = await reader.ReadLineAsync() ?? throw new Exception("CSV is empty");
			var headers = headerLine.Split(delimiter);

			string? line;
			while ((line = await reader.ReadLineAsync()) != null)
			{
				if (string.IsNullOrWhiteSpace(line)) continue;
				var fields = line.Split(delimiter);
				var media = new Media
				{
					Id = int.TryParse(GetField(headers, fields, "id"), out var id) ? id : 0,
					Title = GetField(headers, fields, "title"),
					RemoteUID = GetField(headers, fields, "ruid"),
					FilePath = GetField(headers, fields, "location"),
					MimeType = GetField(headers, fields, "mime"),
					PrimaryArtist = GetField(headers, fields, "primary artist"),
					Album = GetField(headers, fields, "album"),
					Description = GetField(headers, fields, "description"),
					Rating = int.TryParse(GetField(headers, fields, "rating"), out var rating) ? rating : 0,
					LengthMilliseconds = long.TryParse(GetField(headers, fields, "length milliseconds"), out var len) ? len : 0,
					Order = int.TryParse(GetField(headers, fields, "order"), out var order) ? order : mediaList.Count
				};
				mediaList.Add(media);
				bakedEntries.Add(media.Id);
			}
			var playlist = new Playlist
			{
				Title = "Imported CSV",
				Description = "",
				UserQuery = "",
				WhiteList = bakedEntries
			};
			return (playlist, mediaList);

			static string GetField(string[] headers, string[] fields, string name)
			{
				var idx = Array.FindIndex(headers, h => h.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));
				return idx >= 0 && idx < fields.Length ? fields[idx].Trim() : "";
			}
		}
		#endregion
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
