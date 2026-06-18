using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlaylistRepoLib.Models;
using PlaylistRepoLib.Models.DTOs;
using System.Linq.Expressions;
using UserQueries;

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

		private async Task<IActionResult> GetMediaAttributeStatsAsync(
			Expression<Func<Media, string?>> attributeSelector,
			string filter = "",
			string sortBy = "name",
			int limit = 10)
		{
			try
			{
				IQueryable<MediaAttributeStatsDTO> query = db.Medias
					.EvaluateUserQuery(filter)
					.Where(m => m.PlayStats != null)
					.GroupBy(attributeSelector)
					.Select(g => new MediaAttributeStatsDTO
					{
						AttributeValue = g.Key ?? string.Empty,
						MediaCount = g.Count(),
						PlayCount = g.Sum(m => m.PlayStats!.PlayCount),
						LastPlayed = g.Max(m => m.PlayStats!.LastPlayed)
					});

				return Ok(await ApplyMediaAttributeLimitAsync(query, sortBy, limit));
			}
			catch (Exception ex)
			{
				return BadRequest(new { error = "An error occurred while processing the request.", details = ex.Message });
			}
		}

		[HttpGet("albums")]
		public Task<IActionResult> GetAlbumStats(
			[FromQuery] string filter = "",
			[FromQuery] string sortBy = "name",
			[FromQuery] int limit = 10) =>
			GetMediaAttributeStatsAsync(m => m.Album, filter, sortBy, limit);

		[HttpGet("artists")]
		public Task<IActionResult> GetArtistStats(
			[FromQuery] string filter = "",
			[FromQuery] string sortBy = "name",
			[FromQuery] int limit = 10) =>
			GetMediaAttributeStatsAsync(m => m.PrimaryArtist, filter, sortBy, limit);

		[HttpGet("genres")]
		public Task<IActionResult> GetGenreStats(
			[FromQuery] string filter = "",
			[FromQuery] string sortBy = "name",
			[FromQuery] int limit = 10) =>
			GetMediaAttributeStatsAsync(m => m.Genre, filter, sortBy, limit);

		private static IQueryable<MediaAttributeStatsDTO> ApplyMediaAttributeOrdering(
			IQueryable<MediaAttributeStatsDTO> query,
			string sortBy)
		{
			return sortBy.ToLowerInvariant() switch
			{
				"plays" => query.OrderByDescending(x => x.PlayCount).ThenBy(x => x.AttributeValue),
				"lastplayed" => query.OrderByDescending(x => x.LastPlayed).ThenBy(x => x.AttributeValue),
				"count" or "mediacount" => query.OrderByDescending(x => x.MediaCount).ThenBy(x => x.AttributeValue),
				_ => query.OrderBy(x => x.AttributeValue)
			};
		}

		private static async Task<List<MediaAttributeStatsDTO>> ApplyMediaAttributeLimitAsync(
			IQueryable<MediaAttributeStatsDTO> query,
			string sortBy,
			int limit)
		{
			limit = Math.Max(limit, 1);

			var normalizedStats = (await query.ToListAsync())
				.GroupBy(
					x => string.IsNullOrWhiteSpace(x.AttributeValue) ? "Other" : x.AttributeValue,
					StringComparer.OrdinalIgnoreCase)
				.Select(group => new MediaAttributeStatsDTO
				{
					AttributeValue = group.Key,
					MediaCount = group.Sum(x => x.MediaCount),
					PlayCount = group.Sum(x => x.PlayCount),
					LastPlayed = group.Max(x => x.LastPlayed)
				})
				.AsQueryable();

			var orderedStats = ApplyMediaAttributeOrdering(normalizedStats, sortBy).ToList();

			if (orderedStats.Count <= limit)
			{
				return orderedStats;
			}

			if (limit == 1)
			{
				return [CreateOtherStats(orderedStats)];
			}

			var visibleStats = orderedStats.Take(limit - 1).ToList();
			var combinedOtherStats = CreateOtherStats(orderedStats.Skip(limit - 1));

			var existingOther = visibleStats.FirstOrDefault(x =>
				string.Equals(x.AttributeValue, "Other", StringComparison.OrdinalIgnoreCase));

			if (existingOther is not null)
			{
				existingOther.MediaCount += combinedOtherStats.MediaCount;
				existingOther.PlayCount += combinedOtherStats.PlayCount;
				existingOther.LastPlayed = existingOther.LastPlayed > combinedOtherStats.LastPlayed
					? existingOther.LastPlayed
					: combinedOtherStats.LastPlayed;
			}
			else
			{
				visibleStats.Add(combinedOtherStats);
			}

			return visibleStats;
		}

		private static MediaAttributeStatsDTO CreateOtherStats(IEnumerable<MediaAttributeStatsDTO> stats)
		{
			var remainingStats = stats.ToList();

			return new MediaAttributeStatsDTO
			{
				AttributeValue = "Other",
				MediaCount = remainingStats.Sum(x => x.MediaCount),
				PlayCount = remainingStats.Sum(x => x.PlayCount),
				LastPlayed = remainingStats.Max(x => x.LastPlayed)
			};
		}
	}
}
