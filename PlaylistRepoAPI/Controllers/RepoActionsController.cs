using Microsoft.AspNetCore.Mvc;
using PlaylistRepoLib;
using PlaylistRepoLib.Models;

namespace PlaylistRepoAPI.Controllers
{
	/// <summary>
	/// Controller for indirect operations
	/// </summary>
	[ApiController]
	[Route("action")]
	public class RepoActionsController(PlayRepoDbContext db, ITaskService taskService) : ControllerBase
	{
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
				id = taskService.StartTask<IRemoteService>((progress, dl) => dl.Fetch(remote, progress));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return AcceptedAtAction(nameof(Fetch), id);
		}

		[HttpPost("download")]
		public IActionResult Download([FromHeader] int remoteId, [FromBody] string[] mediaUIDs)
		{
			RemotePlaylist? remote = db.RemotePlaylists.FirstOrDefault(r => r.Id == remoteId);
			if (remote == null) return BadRequest("Remote does not exist.");
			Guid id;
			try
			{
				id = taskService.StartTask<IRemoteService>((progress, dl) => dl.Download(remote, mediaUIDs, progress));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return AcceptedAtAction(nameof(Download), id);
		}

		[HttpPost("sync")]
		public IActionResult Sync([FromHeader] int remoteId)
		{
			RemotePlaylist? remote = db.RemotePlaylists.FirstOrDefault(r => r.Id == remoteId);
			if (remote == null) return BadRequest("Remote does not exist.");
			Guid id;
			try
			{
				id = taskService.StartTask<IRemoteService>((progress, dl) => dl.Sync(remote, progress));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return AcceptedAtAction(nameof(Sync), id);
		}
	}
}
