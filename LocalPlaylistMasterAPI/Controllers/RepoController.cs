using LocalPlaylistMasterLib.Models;
using Microsoft.AspNetCore.Mvc;

namespace LocalPlaylistMasterAPI.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class RepoController(PlayRepoDbContext db) : ControllerBase
	{
		[HttpGet("get-media")]
		public IEnumerable<Media> GetMedia([FromQuery] int pageSize, [FromQuery] int currentPage)
		{
			return db.Media.Skip(pageSize * currentPage).Take(pageSize);
		}

		[HttpPost("post-media")]
		public IActionResult PostMedia([FromBody] Media media)
		{
			db.Add(media);
			db.SaveChanges();
			return CreatedAtAction(nameof(Media.Id), media.Id);
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
