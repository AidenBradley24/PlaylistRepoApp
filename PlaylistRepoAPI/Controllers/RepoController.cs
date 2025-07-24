using PlaylistRepoLib;
using PlaylistRepoLib.Models;
using Microsoft.AspNetCore.Mvc;

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
		public IEnumerable<Media> GetMedia([FromQuery] int pageSize, [FromQuery] int currentPage)
		{
			return db.Medias.Skip(pageSize * currentPage).Take(pageSize);
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

			return AcceptedAtAction(nameof(Ingest), id);
		}
	}
}
