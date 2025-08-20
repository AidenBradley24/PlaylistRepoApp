using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistRepoLib.Models;

public class Playlist
{
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	[Key] public int Id { get; set; }
	public string Title { get; set; } = null!;
	public string? Description { get; set; }

	/// <summary>
	/// A query to dynamically add elements to the playlist
	/// </summary>
	public string UserQuery { get; set; } = "";

	/// <summary>
	/// Entries definitively in the playlist
	/// </summary>
	public List<int> BakedEntries { get; set; } = [];
}
