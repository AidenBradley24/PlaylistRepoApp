using Microsoft.AspNetCore.Mvc;
using PlaylistRepoLib.Models;
using PlaylistRepoLib.Models.DTOs;
using UserQueries;

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
				return Ok(new ApiGetResponse<Media, MediaDTO>(db.Medias, query, pageSize, currentPage));
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
			return Ok(new MediaDTO(result));
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

		[HttpPatch("media")]
		public IActionResult PatchMedia([FromBody] MediaDTO.PatchElement patch)
		{
			IQueryable<Media> records;
			try
			{
				records = db.Medias.EvaluateUserQuery(patch.UserQuery);
			}
			catch (InvalidUserQueryException ex)
			{
				return BadRequest($"Invalid user query: {ex.Message}");
			}
			if (!records.Any()) return NotFound();
			List<MediaDTO> modifiedDtos = [];
			foreach (var record in records)
			{
				var dto = new MediaDTO(record);
				if (!dto.Patch(patch))
				{
					db.ChangeTracker.Clear();
					return BadRequest();
				}
				dto.UpdateModel(record);
				modifiedDtos.Add(dto);
			}
			db.SaveChanges();
			return Ok(modifiedDtos);
		}

		[HttpPatch("playlists")]
		public IActionResult PatchPlaylist([FromBody] PlaylistDTO.PatchElement patch)
		{
			IQueryable<Playlist> records;
			try
			{
				records = db.Playlists.EvaluateUserQuery(patch.UserQuery);
			}
			catch (InvalidUserQueryException ex)
			{
				return BadRequest($"Invalid user query: {ex.Message}");
			}
			if (!records.Any()) return NotFound();
			List<PlaylistDTO> modifiedDtos = [];
			foreach (var record in records)
			{
				var dto = new PlaylistDTO(record);
				if (!dto.Patch(patch))
				{
					db.ChangeTracker.Clear();
					return BadRequest();
				}
				dto.UpdateModel(record);
				modifiedDtos.Add(dto);
			}
			db.SaveChanges();
			return Ok(modifiedDtos);
		}

		[HttpPatch("remotes")]
		public IActionResult PatchRemote([FromBody] RemotePlaylistDTO.PatchElement patch)
		{
			IQueryable<RemotePlaylist> records;
			try
			{
				records = db.RemotePlaylists.EvaluateUserQuery(patch.UserQuery);
			}
			catch (InvalidUserQueryException ex)
			{
				return BadRequest($"Invalid user query: {ex.Message}");
			}
			if (!records.Any()) return NotFound();
			List<RemotePlaylistDTO> modifiedDtos = [];
			foreach (var record in records)
			{
				var dto = new RemotePlaylistDTO(record);
				if (!dto.Patch(patch))
				{
					db.ChangeTracker.Clear();
					return BadRequest();
				}
				dto.UpdateModel(record);
				modifiedDtos.Add(dto);
			}
			db.SaveChanges();
			return Ok(modifiedDtos);
		}

		[HttpDelete("media/{id}")]
		public IActionResult DeleteMedia([FromRoute] int id, [FromHeader] bool alsoDeleteFile = false)
		{
			var record = db.Medias.Find(id);
			if (record == null) return NotFound();
			if (alsoDeleteFile) record.File?.Delete();
			db.Medias.Remove(record);
			db.SaveChanges();
			return Ok();
		}

		[HttpDelete("media")]
		public IActionResult DeleteMedias([FromHeader] string query, [FromHeader] bool alsoDeleteFile = false)
		{
			IQueryable<Media> records;
			try
			{
				records = db.Medias.EvaluateUserQuery(query);
			}
			catch (InvalidUserQueryException ex)
			{
				return BadRequest($"Invalid user query: {ex.Message}");
			}

			if (alsoDeleteFile)
				foreach (var record in records)
				{
					record.File?.Delete();
				}

			db.Medias.RemoveRange(records);
			db.SaveChanges();
			return Ok();
		}

		[HttpGet("remotes")]
		public IActionResult GetRemotes([FromQuery] string query = "", [FromQuery] int pageSize = 10, [FromQuery] int currentPage = 0)
		{
			try
			{
				return Ok(new ApiGetResponse<RemotePlaylist, RemotePlaylistDTO>(db.RemotePlaylists, query, pageSize, currentPage));
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
			return Ok(new RemotePlaylistDTO(result));
		}

		[HttpGet("remotes/{id}/media")]
		public IActionResult GetRemoteMedias([FromRoute] int id = 0, [FromQuery] string query = "", [FromQuery] int pageSize = 10, [FromQuery] int currentPage = 0)
		{
			var playlist = db.RemotePlaylists.Find(id);
			if (playlist == null) return NotFound();

			try
			{
				return Ok(new ApiGetResponse<Media, MediaDTO>(playlist.AllEntries(db.Medias, false), query, pageSize, currentPage));
			}
			catch (InvalidUserQueryException ex)
			{
				return BadRequest($"Invalid user query: {ex.Message}");
			}
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

		[HttpDelete("remotes/{id}")]
		public IActionResult DeleteRemote([FromRoute] int id, [FromHeader] bool alsoDeleteMedia = false, [FromHeader] bool alsoDeleteMediaFiles = false)
		{
			var record = db.RemotePlaylists.Find(id);
			if (record == null) return NotFound();
			foreach (var media in record.AllEntries(db.Medias))
			{
				if (alsoDeleteMedia)
				{
					if (alsoDeleteMediaFiles) media.File?.Delete();
					db.Remove(media);
					continue;
				}
				media.RemoteId = null;
			}
			db.RemotePlaylists.Remove(record);
			db.SaveChanges();
			return Ok();
		}

		[HttpGet("playlists")]
		public IActionResult GetPlaylists([FromQuery] string query = "", [FromQuery] int pageSize = 10, [FromQuery] int currentPage = 0)
		{
			try
			{
				return Ok(new ApiGetResponse<Playlist, PlaylistDTO>(db.Playlists, query, pageSize, currentPage));
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
			return Ok(new PlaylistDTO(result));
		}

		[HttpGet("playlists/{id}/media")]
		public IActionResult GetPlaylistMedias([FromRoute] int id = 0, [FromQuery] string query = "", [FromQuery] int pageSize = 10, [FromQuery] int currentPage = 0)
		{
			var playlist = db.Playlists.Find(id);
			if (playlist == null) return NotFound();

			try
			{
				return Ok(new ApiGetResponse<Media, MediaDTO>(playlist.AllEntries(db.Medias, false), query, pageSize, currentPage));
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

		[HttpDelete("playlists/{id}")]
		public IActionResult DeletePlaylist([FromRoute] int id)
		{
			var record = db.Playlists.Find(id);
			if (record == null) return NotFound();
			db.Playlists.Remove(record);
			db.SaveChanges();
			return Ok();
		}
	}
}
