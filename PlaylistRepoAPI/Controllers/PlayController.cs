using Microsoft.AspNetCore.Mvc;

namespace PlaylistRepoAPI.Controllers
{
	[ApiController]
	[Route("play")]
	public class PlayController(PlayRepoDbContext db) : ControllerBase
	{
		[HttpGet("media/{id}")]
		public IActionResult GetFile([FromRoute] int id)
		{
			var media = db.Medias.Find(id);
			if (media == null) return NotFound();
			if (!media.IsOnFile) return NoContent();
			var fs = media.File!.OpenRead();
			return File(fs, media.MimeType, true);
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
			switch (Path.GetExtension(file))
			{
				case ".xspf":
					await playlist.StreamXspfAsync(db.Medias, stream, new PlaylistStreamingSettings() { ApiUrl = apiURL, UseDirectory = false });
					stream.Position = 0;
					return File(stream, "application/xspf+xml");
				case ".m3u8":
					await playlist.StreamM3U8Async(db.Medias, stream, new PlaylistStreamingSettings() { ApiUrl = apiURL, UseDirectory = false });
					stream.Position = 0;
					return File(stream, "application/vnd.apple.mpegurl");
				default:
					return BadRequest(Path.GetExtension(file) + " is not valid.");
			}
		}
	}
}
