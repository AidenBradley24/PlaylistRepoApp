using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistRepoLib.Models;

public class Playlist
{
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	[Key] public int Id { get; set; }
	public string Title { get; set; } = null!;
	public string? Description { get; set; }
	public string UserQuery { get; set; } = "";
}
