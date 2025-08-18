using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistRepoLib.Models;

public class RemotePlaylist
{
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	[Key] public int Id { get; set; }
	public string Name { get; set; } = null!;
	public string? Description { get; set; }
	public string Link { get; set; } = null!;
	public Media.MediaType MediaType { get; set; } = Media.MediaType.undefined;
	public RemoteType Type { get; set; } = RemoteType.internet;

	public enum RemoteType
	{
		internet,
		ytdlp
	}
}
