using Microsoft.AspNetCore.Mvc;
using PlaylistRepoLib.Models;
using PlaylistRepoLib.Models.DTOs;
using PlaylistRepoLib.UserQueries;

namespace PlaylistRepoAPI.Controllers
{
	/// <summary>
	/// Controller for direct data access
	/// </summary>
	[ApiController]
	[Route("api/[controller]")]
	public class DataController(PlayRepoDbContext db) : ControllerBase
	{
		[HttpGet("info")]
		public string GetInfo()
		{
			return $"Media Count: {db.Medias.Count()}";
		}

		[HttpGet("media")]
		public IActionResult GetMedias([FromQuery] string query = "", [FromQuery] int pageSize = 10, [FromQuery] int currentPage = 0)
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

		[HttpGet("media/{id}")]
		public IActionResult GetMedia([FromRoute] int id = 0)
		{
			var result = db.Medias.Find(id);
			if (result == null) return NotFound();
			return Ok(result);
		}

		[HttpPost("media")]
		public IActionResult AddOrUpdateMedia([FromBody] MediaDTO dto)
		{
			var record = db.Medias.Find(dto.Id);
			if (record == null)
			{
				record = new Media();
				dto.UpdateModel(record);
				db.Add(record);
			}
			else
			{
				dto.UpdateModel(record);
			}

			db.SaveChanges();
			dto.SyncDTO(record);
			return Ok(dto);
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
		public IActionResult GetRemotes([FromQuery] string query = "", [FromQuery] int pageSize = 10, [FromQuery] int currentPage = 0)
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

		[HttpGet("remotes/{id}")]
		public IActionResult GetRemote([FromRoute] int id = 0)
		{
			var result = db.RemotePlaylists.Find(id);
			if (result == null) return NotFound();
			return Ok(result);
		}

		[HttpPost("remotes")]
		public IActionResult AddOrUpdateRemote([FromBody] RemotePlaylistDTO dto)
		{
			var record = db.RemotePlaylists.Find(dto.Id);
			if (record == null)
			{
				record = new RemotePlaylist();
				dto.UpdateModel(record);
				db.Add(record);
			}
			else
			{
				dto.UpdateModel(record);
			}

			db.SaveChanges();
			dto.SyncDTO(record);
			return Ok(dto);
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
		public IActionResult GetPlaylists([FromQuery] string query = "", [FromQuery] int pageSize = 10, [FromQuery] int currentPage = 0)
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

		[HttpGet("playlists/{id}")]
		public IActionResult GetPlaylist([FromRoute] int id = 0)
		{
			var result = db.Playlists.Find(id);
			if (result == null) return NotFound();
			return Ok(result);
		}

		[HttpGet("playlists/{id}/media")]
		public IActionResult GetPlaylistMedias([FromRoute] int id = 0, [FromQuery] string query = "", [FromQuery] int pageSize = 10, [FromQuery] int currentPage = 0)
		{
			var playlist = db.Playlists.Find(id);
			if (playlist == null) return NotFound();

			try
			{
				return Ok(new ApiGetResponse<Media>(playlist.AllEntries(db.Medias, false), query, pageSize, currentPage));
			}
			catch (InvalidUserQueryException ex)
			{
				return BadRequest($"Invalid user query: {ex.Message}");
			}
		}

		[HttpPost("playlists")]
		public IActionResult AddOrUpdatePlaylist([FromBody] PlaylistDTO dto)
		{
			var record = db.Playlists.Find(dto.Id);
			if (record == null)
			{
				record = new Playlist();
				dto.UpdateModel(record);
				db.Add(record);
			}
			else
			{
				dto.UpdateModel(record);
			}

			db.SaveChanges();
			dto.SyncDTO(record);
			return Ok(dto);
		}

		[HttpDelete("playlists")]
		public IActionResult DeletePlaylist([FromHeader] int id)
		{
			var playlist = new Playlist { Id = id };
			db.Playlists.Remove(playlist);
			db.SaveChanges();
			return Ok();
		}
	}
}
