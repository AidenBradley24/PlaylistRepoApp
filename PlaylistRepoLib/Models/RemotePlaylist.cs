using System.ComponentModel.DataAnnotations;

namespace PlaylistRepoLib.Models;

public class RemotePlaylist
{
	[Key] public int Id { get; set; }
	public string Name { get; set; } = null!;
	public string? Description { get; set; }
	public string Link { get; set; } = null!;
}
