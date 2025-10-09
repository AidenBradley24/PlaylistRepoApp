using Microsoft.AspNetCore.Mvc;
using PlaylistRepoLib;

namespace PlaylistRepoAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ServiceController(IPlayRepoService repoService, ITaskService taskService) : ControllerBase
	{
		[HttpGet("status")]
		public IActionResult GetStatus()
		{
			if (repoService.IsRepoInitialized)
				return Ok();
			return StatusCode(StatusCodes.Status503ServiceUnavailable);
		}

		[HttpGet("status/{id}")]
		public ActionResult<TaskStatus> GetTaskStatus(Guid id)
		{
			var status = taskService.GetProgress(id);
			if (status == null)
				return NotFound();

			return Ok(status);
		}

		[HttpPost("init")]
		public IActionResult Init()
		{
			bool success = repoService.Initialize();
			if (success) return Ok();
			return BadRequest();
		}

		// TODO add a sandboxed change directory

		[HttpPost("test")]
		public IActionResult Test([FromHeader] int milliseconds)
		{
			if (milliseconds < 0) throw new Exception("Test error");
			var id = taskService.StartTask(async (progress, _) =>
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
