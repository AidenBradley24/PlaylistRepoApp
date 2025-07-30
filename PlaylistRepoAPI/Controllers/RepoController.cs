using Microsoft.AspNetCore.Mvc;
using PlaylistRepoLib;
using PlaylistRepoLib.Models;
using PlaylistRepoLib.UserQueries;

namespace PlaylistRepoAPI.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class RepoController(PlayRepoDbContext db, ITaskService taskService) : ControllerBase
	{
		[HttpGet("get-info")]
		public string GetInfo()
		{
			return $"Media Count: {db.Medias.Count()}";
		}

		[HttpGet("get-media")]
		public IActionResult GetMedia([FromQuery] int pageSize = 10, [FromQuery] int currentPage = 0, [FromQuery] string query = "")
		{
			try
			{
				return Ok(db.Medias.EvaluateUserQuery(query).Skip(pageSize * currentPage).Take(pageSize));
			}
			catch (InvalidUserQueryException ex)
			{
				return BadRequest($"Invalid user query: {ex.Message}");
			}
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
				id = taskService.StartTaskWithDb((progress, db) => db.IngestUntracked([.. files], progress));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return AcceptedAtAction(nameof(Ingest), id);
		}

		[HttpGet("file")]
		public IActionResult GetFile([FromQuery] int mediaId)
		{
			var media = db.Medias.Find(mediaId);
			if (media == null) return NotFound();
			using var tagFile = media.GetTagFile();
			var fs = media.File!.OpenRead();
			return File(fs, tagFile.MimeType, true);
		}
	}
}
