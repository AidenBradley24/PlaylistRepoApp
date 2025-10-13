using Microsoft.AspNetCore.Mvc;
using PlaylistRepoLib.Models;
using PlaylistRepoLib.Models.DTOs;

namespace PlaylistRepoAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class MetadataController(PlayRepoService repo, PlayRepoDbContext db, MetadataEnrichmentService service) : ControllerBase
	{
		[HttpPost("enrich/{id}")]
		public async Task<IActionResult> Enrich([FromRoute] int id)
		{
			var media = db.Medias.Find(id);
			if (media == null) return NotFound();
			var result = await service.EnrichMedia(media.GetDTO());
			result?.UpdateModel(media);
			db.SaveChanges();
			return Ok(result);
		}

		[HttpPost("autoname/{id}")]
		public IActionResult AutoNameAndMetadata([FromRoute] int id)
		{
			Media? media = db.Medias.Find(id);
			if (media == null) return NotFound();
			FileInfo? file = media.File;
			if (file == null) return NoContent();
			string newFileName = media.GenerateFileName(file.Extension);
			file.MoveTo(Path.Combine(file.DirectoryName ?? "", newFileName));
			media.FilePath = repo.GetRelativePath(file);
			media.SyncToMediaFile();
			db.SaveChanges();
			return Ok(media.FilePath);
		}
	}
}
