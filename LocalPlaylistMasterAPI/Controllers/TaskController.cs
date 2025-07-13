using Microsoft.AspNetCore.Mvc;

namespace LocalPlaylistMasterAPI.Controllers
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
	}
}
