using Microsoft.AspNetCore.Mvc;
using PlaylistRepoLib;

namespace PlaylistRepoAPI.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class TaskController(ITaskService taskService) : ControllerBase
	{
		[HttpGet("status/{id}")]
		public ActionResult<TaskStatus> Status(Guid id)
		{
			var status = taskService.GetProgress(id);
			if (status == null)
				return NotFound();

			return Ok(status);
		}

		[HttpPost("test")]
		public IActionResult Test([FromQuery] int milliseconds)
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

			return AcceptedAtAction(nameof(Test), id);
		}
	}
}
