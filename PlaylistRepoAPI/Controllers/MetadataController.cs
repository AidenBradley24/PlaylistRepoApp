using Microsoft.AspNetCore.Mvc;
using PlaylistRepoLib.Models.DTOs;
using UserQueries;

namespace PlaylistRepoAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class MetadataController(IPlayRepoService repo, PlayRepoDbContext db, MetadataEnrichmentService service) : ControllerBase
	{
		[HttpPost("enrich")]
		public async Task<IActionResult> Enrich([FromHeader] string query = "")
		{
			var medias = db.Medias.EvaluateUserQuery(query).Where(m => !m.Locked);
			List<MediaDTO> dtos = [];
			foreach (var media in medias)
			{
				var dto = media.GetDTO();
				dto = await service.EnrichMedia(dto);
				if (dto == null) continue;
				dto.UpdateModel(media);
				dtos.Add(dto);
			}

			db.SaveChanges();
			return Ok(dtos);
		}

		[HttpPost("autoname")]
		public IActionResult AutoNameAndMetadata([FromHeader] string query = "")
		{
			var medias = db.Medias.EvaluateUserQuery(query);
			List<string> renamedMedias = [];
			foreach (var media in medias)
			{
				if (!media.IsOnFile) continue;
				FileInfo file = media.File!;
				string newFileName = media.GenerateFileName(file.Extension);
				file.MoveTo(Path.Combine(file.DirectoryName ?? "", newFileName));
				media.FilePath = repo.GetRelativePath(file);
				media.SyncToMediaFile();
				renamedMedias.Add(media.FilePath);
			}

			db.SaveChanges();
			return Ok(renamedMedias);
		}
	}
}
