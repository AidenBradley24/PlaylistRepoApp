using PlaylistRepoLib.Models.DTOs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistRepoLib.Models;

public class MediaPlayStats : IHasDTO<MediaPlayStats, MediaPlayStatsDTO>
{
	[Key]
	[ForeignKey(nameof(Media))]
	public int MediaId { get; set; }

	public Media Media { get; set; } = null!;

	
	public int PlayCount { get; set; } = 0;
	public DateTime LastPlayed { get; set; } = DateTime.MinValue;
}
