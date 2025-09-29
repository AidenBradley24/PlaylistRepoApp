using UserQueries;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace PlaylistRepoLib.Models;

[PrimaryUserQueryable(nameof(Title))]
public class Playlist
{
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	[UserQueryable("id")]
	[Key] public int Id { get; set; }

	[UserQueryable("title")]
	public string Title { get; set; } = "";

	[UserQueryable("description")]
	public string Description { get; set; } = "";

	/// <summary>
	/// A query to dynamically add elements to the playlist
	/// </summary>
	public string UserQuery { get; set; } = "";

	/// <summary>
	/// Entries definitively in the playlist
	/// </summary>
	public List<int> BakedEntries { get; set; } = [];

	public string GenerateFileName(string extension)
	{
		StringBuilder sb = new(Title);
		if (!extension.StartsWith('.'))
			sb.Append('.');
		sb.Append(extension);
		return sb.ToString();
	}
}
