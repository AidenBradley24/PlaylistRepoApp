namespace PlaylistRepoLib.Models.DTOs
{
	public class PlaylistDTO : DataTransferObject<Playlist>
	{
		public int Id { get; set; }
		public string Title { get; set; } = "";
		public string Description { get; set; } = "";
		public string UserQuery { get; set; } = "";
		public List<int> BakedEntries { get; set; } = [];

		public PlaylistDTO() { }

		public PlaylistDTO(Playlist model) : this()
		{
			SyncDTO(model);
		}
	}
}
