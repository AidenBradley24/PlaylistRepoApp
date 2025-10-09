﻿namespace PlaylistRepoLib.Models.DTOs
{
	public class PlaylistDTO : DataTransferObject<Playlist>
	{
		public int Id { get; set; }
		public string Title { get; set; } = "";
		public string Description { get; set; } = "";
		public string UserQuery { get; set; } = "";

		public PlaylistDTO() { }

		public PlaylistDTO(Playlist model) : this()
		{
			SyncDTO(model);
		}

		public PlaylistDTO Clone(int? id = null)
		{
			return new PlaylistDTO()
			{
				Id = id ?? Id,
				Title = Title,
				Description = Description,
				UserQuery = UserQuery,
			};
		}
	}
}
