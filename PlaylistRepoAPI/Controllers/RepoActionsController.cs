using Microsoft.AspNetCore.Mvc;
using PlaylistRepoLib;
using PlaylistRepoLib.Models;

namespace PlaylistRepoAPI.Controllers
{
	[ApiController]
	[Route("action")]
	public class RepoActionsController(PlayRepoDbContext db, ITaskService taskService) : ControllerBase
	{
		[HttpPut("update-media")]
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

		[HttpPost("update-remote")]
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

		[HttpPost("update-playlist")]
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

		[HttpPost("ingest")]
		public IActionResult Ingest([FromBody] string fileSpec)
		{
			FileSpec files;
			Guid id;

			try
			{
				files = new FileSpec(fileSpec);
				id = taskService.StartTask<PlayRepoDbContext>((progress, db) => db.IngestUntracked([.. files], progress));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return AcceptedAtAction(nameof(Ingest), id);
		}

		[HttpPost("fetch")]
		public IActionResult Fetch([FromHeader] int remoteId)
		{
			RemotePlaylist? remote = db.RemotePlaylists.FirstOrDefault(r => r.Id == remoteId);
			if (remote == null) return BadRequest("Remote does not exist.");
			Guid id;
			try
			{
				id = taskService.StartTask<YtDlpService>((progress, dl) => dl.Fetch(remote, progress));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return AcceptedAtAction(nameof(Fetch), id);
		}

		[HttpPost("download")]
		public IActionResult Download([FromHeader] int remoteId, [FromHeader] string downloadFileExtension, [FromBody] string[] mediaUIDs)
		{
			RemotePlaylist? remote = db.RemotePlaylists.FirstOrDefault(r => r.Id == remoteId);
			if (remote == null) return BadRequest("Remote does not exist.");
			Guid id;
			try
			{
				id = taskService.StartTask<YtDlpService>((progress, dl) => dl.Download(remote, mediaUIDs, downloadFileExtension, progress));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return AcceptedAtAction(nameof(Download), id);
		}

		[HttpPost("sync")]
		public IActionResult Sync([FromHeader] int remoteId, [FromHeader] string downloadFileExtension)
		{
			RemotePlaylist? remote = db.RemotePlaylists.FirstOrDefault(r => r.Id == remoteId);
			if (remote == null) return BadRequest("Remote does not exist.");
			Guid id;
			try
			{
				id = taskService.StartTask<YtDlpService>((progress, dl) => dl.Sync(remote, downloadFileExtension, progress));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return AcceptedAtAction(nameof(Sync), id);
		}
	}
}
