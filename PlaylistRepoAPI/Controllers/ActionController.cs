using Microsoft.AspNetCore.Mvc;
using PlaylistRepoLib;
using PlaylistRepoLib.Models;

namespace PlaylistRepoAPI.Controllers
{
	/// <summary>
	/// Controller for indirect operations
	/// </summary>
	[ApiController]
	[Route("api/[controller]")]
	public class ActionController(PlayRepoDbContext db, ITaskService taskService) : ControllerBase
	{
		[HttpPost("ingest")]
		public IActionResult Ingest([FromBody] string fileSpec)
		{
			FileSpec files;
			Guid id;

			try
			{
				files = new FileSpec(fileSpec);
				id = taskService.StartTask<PlayRepoDbContext>((progress, _, db) => db.IngestUntracked([.. files], progress));
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
				id = taskService.StartTask<IRemoteService>((progress, _, dl) => dl.Fetch(remote, progress));
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
				id = taskService.StartTask<IRemoteService>((progress, _, dl) => dl.Download(remote, mediaUIDs, progress));
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
				id = taskService.StartTask<IRemoteService>((progress, _, dl) => dl.Sync(remote, progress));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return AcceptedAtAction(nameof(Sync), id);
		}

		[HttpPost("upload")]
		public async Task<IActionResult> UploadMedia(IEnumerable<IFormFile> files)
		{
			Queue<(string fileName, MemoryStream ms)> uploads = [];
			foreach (var file in files)
			{
				using var rs = file.OpenReadStream();
				var ms = new MemoryStream();
				await rs.CopyToAsync(ms);
				ms.Position = 0;
				uploads.Enqueue((file.FileName, ms));
			}

			Guid id;
			try
			{
				id = taskService.StartTask<PlayRepoDbContext, IPlayRepoService>(async (progress, _, db, repo) =>
				{
					List<FileInfo> savedFiles = [];
					int index = 0;
					int count = uploads.Count;

					while (uploads.TryDequeue(out var item))
					{
						var (fileName, ms) = item;
						FileInfo savedFile = new(Path.Combine(repo.RootPath.FullName, fileName));
						savedFiles.Add(savedFile);
						using var fs = savedFile.OpenWrite();
						await ms.CopyToAsync(fs);
						await ms.DisposeAsync();
						progress.Report(TaskProgress.FromNumbers(index, count, "Saving"));
					}

					await db.IngestUntracked(savedFiles, progress);
				});
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return AcceptedAtAction(nameof(UploadMedia), id);
		}
	}
}
