using Microsoft.AspNetCore.Mvc;
using PlaylistRepoLib.Models.DTOs;

namespace PlaylistRepoAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class EnrichmentController(MetadataEnrichmentService service) : ControllerBase
	{
		[HttpPost]
		public async Task<IActionResult> Enrich([FromBody] MediaDTO dto)
		{
			var result = await service.EnrichMedia(dto);
			if (result == null) return NoContent();
			return Ok(result);
		}
	}
}
