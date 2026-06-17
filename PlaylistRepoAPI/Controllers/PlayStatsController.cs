using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlaylistRepoLib;
using PlaylistRepoLib.Models;
using PlaylistRepoLib.Models.DTOs;

namespace PlaylistRepoAPI.Controllers
{
	[ApiController]
	[Route("api/stats")]
	public class PlayStatsController(PlayRepoDbContext db) : ControllerBase
	{
		[HttpGet("media/{id}")]
		public IActionResult GetStats(int id)
		{
			var media = db.Medias
				.Include(m => m.PlayStats)
				.FirstOrDefault(m => m.Id == id);

			if (media == null) return NotFound();
			var stats = media.PlayStats ?? new MediaPlayStats { MediaId = id, PlayCount = 0, LastPlayed = default };
			return Ok(new MediaPlayStatsDTO(stats));
		}

		[HttpGet("media/recent")]
		public async Task<IActionResult> GetRecent()
		{
			var recentMedia = await db.MediaPlayStats
				.OrderByDescending(s => s.LastPlayed)
				.Take(20)
				.Select(s => s.Media)
				.ToListAsync();

			var response = new ApiGetResponse<Media, MediaDTO>() { Data = [.. recentMedia.Select(m => new MediaDTO(m))], Total = 20 };
			return Ok(response);
		}

		class MediaAttributeStatsDTO
		{
			public string AttributeName { get; set; } = "";
			public int MediaCount { get; set; }
			public int PlayCount { get; set; }
			public DateTime? LastPlayed { get; set; }
		}

		class MediaAttributeStatsDTOComparer : IComparer<MediaAttributeStatsDTO>
		{
			private readonly string sortBy;
			public MediaAttributeStatsDTOComparer(string sortBy)
			{
				this.sortBy = sortBy.ToLower();
			}
			public int Compare(MediaAttributeStatsDTO? x, MediaAttributeStatsDTO? y)
			{
				if (x == null || y == null) return 0;
				return sortBy switch
				{
					"plays" => y.PlayCount.CompareTo(x.PlayCount),
					"lastplayed" => Nullable.Compare(y.LastPlayed, x.LastPlayed),
					_ => string.Compare(x.AttributeName, y.AttributeName, StringComparison.OrdinalIgnoreCase)
				};
			}
		}


		[HttpGet("albums")]
		public async Task<IActionResult> GetAlbumStats([FromQuery] string sortBy = "name")
		{
			IQueryable<MediaAttributeStatsDTO> query = db.Medias
				.Where(m => m.PlayStats != null)
				.GroupBy(m => m.Album)
				.Select(g => new MediaAttributeStatsDTO
				{
					AttributeName = g.Key,
					MediaCount = g.Count(),
					PlayCount = g.Sum(m => m.PlayStats!.PlayCount),
					LastPlayed = g.Max(m => m.PlayStats!.LastPlayed)
				});

			return Ok(await ApplyMediaAttributeOrdering(query, sortBy).ToListAsync());
		}

		[HttpGet("genres")]
		public async Task<IActionResult> GetGenreStats([FromQuery] string sortBy = "name")
		{
			IQueryable<MediaAttributeStatsDTO> query = db.Medias
				.Where(m => m.PlayStats != null)
				.GroupBy(m => m.Genre)
				.Select(g => new MediaAttributeStatsDTO
				{
					AttributeName = g.Key,
					MediaCount = g.Count(),
					PlayCount = g.Sum(m => m.PlayStats!.PlayCount),
					LastPlayed = g.Max(m => m.PlayStats!.LastPlayed)
				});

			return Ok(await ApplyMediaAttributeOrdering(query, sortBy).ToListAsync());
		}

		[HttpGet("artists")]
		public async Task<IActionResult> GetArtistStats([FromQuery] string sortBy = "name")
		{
			IQueryable<MediaAttributeStatsDTO> query = db.Medias
				.Where(m => m.PlayStats != null)
				.GroupBy(m => m.PrimaryArtist)
				.Select(g => new MediaAttributeStatsDTO
				{
					AttributeName = g.Key,
					MediaCount = g.Count(),
					PlayCount = g.Sum(m => m.PlayStats!.PlayCount),
					LastPlayed = g.Max(m => m.PlayStats!.LastPlayed)
				});

			return Ok(await ApplyMediaAttributeOrdering(query, sortBy).ToListAsync());
		}

		private static IQueryable<MediaAttributeStatsDTO> ApplyMediaAttributeOrdering(
			IQueryable<MediaAttributeStatsDTO> query,
			string sortBy)
		{
			return sortBy.ToLowerInvariant() switch
			{
				"plays" => query.OrderByDescending(x => x.PlayCount).ThenBy(x => x.AttributeName),
				"lastplayed" => query.OrderByDescending(x => x.LastPlayed).ThenBy(x => x.AttributeName),
				"count" or "mediacount" => query.OrderByDescending(x => x.MediaCount).ThenBy(x => x.AttributeName),
				_ => query.OrderBy(x => x.AttributeName)
			};
		}
	}
}
