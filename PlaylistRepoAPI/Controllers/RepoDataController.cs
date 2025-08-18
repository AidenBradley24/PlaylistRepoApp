using Microsoft.AspNetCore.Mvc;
using PlaylistRepoLib.Models;
using PlaylistRepoLib.UserQueries;

namespace PlaylistRepoAPI.Controllers
{
	/// <summary>
	/// Controller for direct data access
	/// </summary>
	[ApiController]
	[Route("data")]
	public class RepoDataController(PlayRepoDbContext db) : ControllerBase
	{
		[HttpGet("info")]
		public string GetInfo()
		{
			return $"Media Count: {db.Medias.Count()}";
		}

		[HttpGet("media")]
		public IActionResult GetMedia([FromQuery] string query = "", [FromQuery] int pageSize = 10, [FromQuery] int currentPage = 0)
		{
			try
			{
				return Ok(new ApiGetResponse<Media>(db.Medias, query, pageSize, currentPage));
			}
			catch (InvalidUserQueryException ex)
			{
				return BadRequest($"Invalid user query: {ex.Message}");
			}
		}

		[HttpPost("media")]
		public IActionResult AddOrUpdateMedia([FromBody] Media media)
		{
			Media? existing = db.Medias.FirstOrDefault(m => m.Id == media.Id);
			if (existing == null)
				db.Add(media);
			else
				db.Update(media);

			db.SaveChanges();
			return Ok();
		}

		[HttpDelete("media")]
		public IActionResult DeleteMedia([FromHeader] int id)
		{
			var media = new Media { Id = id };
			db.Medias.Remove(media);
			db.SaveChanges();
			return Ok();
		}

		[HttpGet("remotes")]
		public IActionResult GetRemote([FromQuery] string query = "", [FromQuery] int pageSize = 10, [FromQuery] int currentPage = 0)
		{
			try
			{
				return Ok(new ApiGetResponse<RemotePlaylist>(db.RemotePlaylists, query, pageSize, currentPage));
			}
			catch (InvalidUserQueryException ex)
			{
				return BadRequest($"Invalid user query: {ex.Message}");
			}
		}

		[HttpPost("remotes")]
		public IActionResult AddOrUpdateRemote([FromBody] RemotePlaylist remotePlaylist)
		{
			RemotePlaylist? existing = db.RemotePlaylists.FirstOrDefault(m => m.Id == remotePlaylist.Id);
			if (existing == null)
				db.Add(remotePlaylist);
			else
				db.Update(remotePlaylist);

			db.SaveChanges();
			return Ok();
		}

		[HttpDelete("remotes")]
		public IActionResult DeleteRemote([FromHeader] int id)
		{
			var remote = new RemotePlaylist { Id = id };
			db.RemotePlaylists.Remove(remote);
			db.SaveChanges();
			return Ok();
		}

		[HttpGet("playlists")]
		public IActionResult GetPlaylist([FromQuery] string query = "", [FromQuery] int pageSize = 10, [FromQuery] int currentPage = 0)
		{
			try
			{
				return Ok(new ApiGetResponse<Playlist>(db.Playlists, query, pageSize, currentPage));
			}
			catch (InvalidUserQueryException ex)
			{
				return BadRequest($"Invalid user query: {ex.Message}");
			}
		}

		[HttpPost("playlists")]
		public IActionResult AddOrUpdatePlaylist([FromBody] Playlist playlist)
		{
			Playlist? existing = db.Playlists.FirstOrDefault(m => m.Id == playlist.Id);
			if (existing == null)
				db.Add(playlist);
			else
				db.Update(playlist);

			db.SaveChanges();
			return Ok();
		}

		[HttpDelete("playlists")]
		public IActionResult DeletePlaylist([FromHeader] int id)
		{
			var playlist = new Playlist { Id = id };
			db.Playlists.Remove(playlist);
			db.SaveChanges();
			return Ok();
		}

		[HttpGet("media/{id}")]
		public IActionResult GetFile([FromRoute] int id)
		{
			var media = db.Medias.Find(id);
			if (media == null) return NotFound();
			var fs = media.File!.OpenRead();
			return File(fs, media.MimeType, true);
		}
	}
}
