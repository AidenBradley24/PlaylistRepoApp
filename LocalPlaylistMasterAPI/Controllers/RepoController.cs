using LocalPlaylistMasterLib;
using LocalPlaylistMasterLib.Models;
using Microsoft.AspNetCore.Mvc;

namespace LocalPlaylistMasterAPI.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class RepoController(PlayRepoDbContext db, ITaskService taskService) : ControllerBase
	{
		[HttpGet("get-media")]
		public IEnumerable<Media> GetMedia([FromQuery] int pageSize, [FromQuery] int currentPage)
		{
			return db.Media.Skip(pageSize * currentPage).Take(pageSize);
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

				progress.Report(TaskProgress.CompletedTask);
			});

			return CreatedAtAction(nameof(TestDelay), id);
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
	}
}
