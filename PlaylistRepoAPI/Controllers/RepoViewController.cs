using Microsoft.AspNetCore.Mvc;
using PlaylistRepoLib.UserQueries;

namespace PlaylistRepoAPI.Controllers
{
	[ApiController]
	[Route("view")]
	public class RepoViewController(PlayRepoDbContext db) : ControllerBase
	{
		[HttpGet("info")]
		public string GetInfo()
		{
			return $"Media Count: {db.Medias.Count()}";
		}

		[HttpGet("media")]
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
