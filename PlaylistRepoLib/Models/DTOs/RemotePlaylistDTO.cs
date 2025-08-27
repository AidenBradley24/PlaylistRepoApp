namespace PlaylistRepoLib.Models.DTOs
{
	public class RemotePlaylistDTO : DataTransferObject<RemotePlaylist>
	{
		public int Id { get; set; }
		public string Name { get; set; } = "unnamed remote playlist";
		public string Description { get; set; } = "";
	}
}
