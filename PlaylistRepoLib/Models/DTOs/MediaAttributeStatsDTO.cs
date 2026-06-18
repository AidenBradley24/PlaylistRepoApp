namespace PlaylistRepoLib.Models.DTOs
{
	public class MediaAttributeStatsDTO
	{
		public string AttributeValue { get; set; } = "";
		public int MediaCount { get; set; }
		public int PlayCount { get; set; }
		public DateTime? LastPlayed { get; set; }
	}

	public class MediaAttributeStatsDTOComparer : IComparer<MediaAttributeStatsDTO>
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
				_ => string.Compare(x.AttributeValue, y.AttributeValue, StringComparison.OrdinalIgnoreCase)
			};
		}
	}
}
