namespace PlaylistRepoLib.Models.DTOs
{
	public class MediaDTO : DataTransferObject<Media>
	{
		public int Id { get; set; }
		public bool IsOnFile { get; set; }
		public string MimeType { get; set; } = "";
		public string Title { get; set; } = "unnamed media";
		public string PrimaryArtist { get; set; } = "";
		public string[]? Artists { get; set; }
		public string Genre { get; set; } = "";
		public string Album { get; set; } = "";
		public string Description { get; set; } = "";
		public int Rating { get; set; } = 0;
		public long LengthMilliseconds { get; set; } = 0;
		public int Order { get; set; } = 0;
		public bool Locked { get; set; } = false;

		public MediaDTO() { }

		public MediaDTO(Media model) : this()
		{
			SyncDTO(model);
		}

		public MediaDTO Clone(int? id = null)
		{
			return new MediaDTO()
			{
				Id = id ?? Id,
				IsOnFile = IsOnFile,
				MimeType = MimeType,
				Title = Title,
				PrimaryArtist = PrimaryArtist,
				Artists = Artists == null ? [] : [.. Artists],
				Genre = Genre,
				Album = Album,
				Description = Description,
				Rating = Rating,
				LengthMilliseconds = LengthMilliseconds,
				Order = Order,
				Locked = Locked,
			};
		}
	}
}
