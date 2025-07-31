using System.ComponentModel.DataAnnotations;

namespace PlaylistRepoLib.Models;

public class Playlist
{
	[Key] public int Id { get; set; }
	public string Title { get; set; } = null!;
	public string? Description { get; set; }
	public string UserQuery { get; set; } = "";
}
