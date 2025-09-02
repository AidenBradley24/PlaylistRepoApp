namespace PlaylistRepoLib.Models.DTOs
{
	public class RemotePlaylistDTO : DataTransferObject<RemotePlaylist>
	{
		public int Id { get; set; }
		public string Name { get; set; } = "unnamed remote playlist";
		public string Description { get; set; } = "";
		public string Link { get; set; } = "";

		public string Type { get; set; } = "internet";

		public override void OnSyncDTO(RemotePlaylist model)
		{
			Type = Enum.GetName(model.Type) ?? "";
		}

		public override void OnUpdateModel(RemotePlaylist model)
		{
			model.Type = Enum.Parse<RemotePlaylist.RemoteType>(Type);
		}

		public RemotePlaylistDTO() { }

		public RemotePlaylistDTO(RemotePlaylist model) : this()
		{
			SyncDTO(model);
		}
	}
}
