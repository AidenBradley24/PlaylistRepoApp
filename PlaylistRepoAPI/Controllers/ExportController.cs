using Microsoft.AspNetCore.Mvc;
using PlaylistRepoLib;

namespace PlaylistRepoAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ExportController(PlayRepoDbContext db, IExportService exportService, ITaskService taskService) : ControllerBase
	{
		[HttpGet("result/{taskId}")]
		public IActionResult GetFile([FromRoute] Guid taskId)
		{
			if (!exportService.TryGetExport(taskId, out var record)) return NotFound();
			if (record.File == null) return Accepted();
			return File(record.File.OpenRead(), MimeTypes.GetMimeType(record.ExportName), record.ExportName);
		}

		[HttpGet("playlist/{file}")]
		public IActionResult StartExport([FromRoute] string file)
		{
			if (!int.TryParse(Path.GetFileNameWithoutExtension(file), out int playlistId))
				return BadRequest("Invalid playlist id number.");

			var playlist = db.Playlists.Find(playlistId);
			if (playlist == null) return NotFound();

			Guid id;
			try
			{
				id = taskService.StartTask<IExportService>((progress, guid, exportService) => exportService.ExportZip(playlist, guid, progress));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return AcceptedAtAction(nameof(StartExport), id);
		}

	}
}
