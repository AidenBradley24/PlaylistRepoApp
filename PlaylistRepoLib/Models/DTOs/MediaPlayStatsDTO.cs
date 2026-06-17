namespace PlaylistRepoLib.Models.DTOs
{
	public class MediaPlayStatsDTO : DataTransferObject<MediaPlayStats>
	{
		public int MediaId { get; set; }
		public int PlayCount { get; set; }
		public DateTime LastPlayed { get; set; }
		public MediaPlayStatsDTO() { }
		public MediaPlayStatsDTO(MediaPlayStats model) : this()
		{
			SyncDTO(model);
		}
	}
}
