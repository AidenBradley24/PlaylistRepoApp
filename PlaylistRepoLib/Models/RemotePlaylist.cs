using System.ComponentModel.DataAnnotations;

namespace PlaylistRepoLib.Models;

public class RemotePlaylist
{
	[Key] public string Name { get; set; } = null!;
	public string? Description { get; set; }
	public required string Link { get; set; }
}
