using UserQueries;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistRepoLib.Models;

[PrimaryUserQueryable(nameof(Name))]
public class RemotePlaylist
{
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]

	[UserQueryable("id")]
	[Key] public int Id { get; set; }

	[UserQueryable("name")]
	public string Name { get; set; } = "unnamed remote playlist";

	[UserQueryable("description")]
	public string Description { get; set; } = "";

	public string Link { get; set; } = "";
	public string MediaMime { get; set; } = "";

	public RemoteType Type { get; set; } = RemoteType.internet;

	public enum RemoteType
	{
		internet,
		ytdlp
	}
}
