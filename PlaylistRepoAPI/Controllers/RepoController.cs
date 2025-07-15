using PlaylistRepoLib;
using PlaylistRepoLib.Models;
using Microsoft.AspNetCore.Mvc;

namespace PlaylistRepoAPI.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class RepoController(PlayRepoDbContext db, ITaskService taskService) : ControllerBase
	{
		[HttpGet("get-media")]
		public IEnumerable<Media> GetMedia([FromQuery] int pageSize, [FromQuery] int currentPage)
		{
			return db.Medias.Skip(pageSize * currentPage).Take(pageSize);
		}

		[HttpPost("test")]
		public IActionResult TestDelay([FromQuery] int milliseconds)
		{
			var id = taskService.StartTask(async (progress) =>
			{
				int remaining = milliseconds;
				while (remaining > 0)
				{
					remaining -= 1000;
					await Task.Delay(1000);
					progress.Report(new TaskProgress { Progress = 100 * (milliseconds - remaining) / milliseconds, Status = "Running" });
				}

				progress.Report(TaskProgress.FromCompleted());
			});

			return AcceptedAtAction(nameof(TestDelay), id);
		}

		[HttpPut("update-media")]
		public IActionResult UpdateMedia([FromBody] Media media)
		{
			db.Update(media);
			db.SaveChanges();
			return Ok();
		}

		[HttpPut("update-multiple-media")]
		public IActionResult UpdateMultipleMedia([FromBody] Media[] media)
		{
			db.UpdateRange(media);
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
				id = taskService.StartTaskWithDb((progress, db) => db.Ingest([.. files], progress));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return AcceptedAtAction(nameof(TestDelay), id);
		}
	}
}
