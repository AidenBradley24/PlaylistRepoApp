using Microsoft.AspNetCore.Mvc;
using PlaylistRepoLib.Models;

namespace PlaylistRepoAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class PlayController(PlayRepoDbContext db) : ControllerBase
	{
		/// <summary>
		/// Play the media file, and update the play stats for the media.
		/// </summary>
		[HttpGet("media/{id}")]
		public IActionResult GetFile([FromRoute] int id)
		{
			var media = db.Medias.Find(id);
			if (media == null) return NotFound();
			if (!media.IsOnFile) return NoContent();

			var playStatsRef = db.Entry(media).Reference(m => m.PlayStats);
			if (!playStatsRef.IsLoaded)
			{
				playStatsRef.Load();
			}

			var playStats = playStatsRef.CurrentValue;
			if (playStats == null)
			{
				playStats = new MediaPlayStats
				{
					Media = media
				};

				db.Add(playStats);
				media.PlayStats = playStats;
			}

			if (playStats.LastPlayed == default || DateTime.UtcNow - playStats.LastPlayed > TimeSpan.FromMilliseconds(media.LengthMilliseconds))
			{
				playStats.PlayCount++;
				playStats.LastPlayed = DateTime.UtcNow;
				db.SaveChanges();
			}

			var fs = media.File!.OpenRead();
			return File(fs, media.MimeType, enableRangeProcessing: true);
		}

		/// <summary>
		/// Preview the media file, and do not update the play stats for the media.
		/// </summary>
		[HttpGet("media/preview/{id}")]
		public IActionResult PreviewFile([FromRoute] int id)
		{
			var media = db.Medias.Find(id);
			if (media == null) return NotFound();
			if (!media.IsOnFile) return NoContent();
			var fs = media.File!.OpenRead();
			if (media.MimeType.StartsWith("text"))
			{
				return File(fs, "text/plain", enableRangeProcessing: true);
			}
			return File(fs, media.MimeType, enableRangeProcessing: true);
		}

		[HttpGet("playlist/{file}")]
		public async Task<IActionResult> GetPlaylistAync([FromRoute] string file)
		{
			if (!int.TryParse(Path.GetFileNameWithoutExtension(file), out int playlistId))
				return BadRequest("Invalid playlist id number.");

			var playlist = db.Playlists.Find(playlistId);
			if (playlist == null) return NotFound();

			var stream = new MemoryStream();
			string apiURL = (Request.IsHttps ? "https://" : "http://") + Request.Host.Value;
			string extension = Path.GetExtension(file);
			if (string.IsNullOrWhiteSpace(extension)) return BadRequest($"Include an extension after the file name. i.e. '{file}.xspf'");
			switch (extension)
			{
				case ".xspf":
					await playlist.StreamXspfAsync(db.Medias, stream, new PlaylistStreamingSettings() { ApiUrl = apiURL, UseDirectory = false });
					stream.Position = 0;
					return File(stream, "application/xspf+xml");
				case ".m3u8":
					await playlist.StreamM3U8Async(db.Medias, stream, new PlaylistStreamingSettings() { ApiUrl = apiURL, UseDirectory = false });
					stream.Position = 0;
					return File(stream, "application/vnd.apple.mpegurl");
				case ".csv":
					await playlist.StreamCSVAsync(db.Medias, stream, new PlaylistStreamingSettings() { ApiUrl = apiURL, UseDirectory = false }, ',');
					stream.Position = 0;
					return File(stream, "text/csv");
				default:
					return BadRequest($"Extension: '{extension}' is not valid.");
			}
		}
	}
}
