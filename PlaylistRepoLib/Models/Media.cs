using PlaylistRepoLib.UserQueries;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.RegularExpressions;

namespace PlaylistRepoLib.Models;

[PrimaryUserQueryable(nameof(Title))]
public partial class Media
{
	[Key] public int Id { get; set; }

	[ForeignKey(nameof(Source))] public int? RemoteSource { get; set; }
	public RemotePlaylist? Source { get; set; }

	/// <summary>
	/// A unique identifier for this media on any remote server.
	/// </summary>
	public string? RemoteUID { get; set; }

	[NotMapped] public FileInfo? File => FilePath != null ? new(FilePath) : null;

	public string? FilePath { get; set; }
	public byte[]? Hash { get; set; }

	[UserQueryable("title")]
	public string Title { get; set; } = "unnamed media";

	[UserQueryable("artist")]
	[NotMapped]
	public string? PrimaryArtist => Artists?.FirstOrDefault();

	public string[]? Artists { get; set; }

	[UserQueryable("album")]
	public string? Album { get; set; }

	[UserQueryable("description")]
	public string? Description { get; set; }

	[UserQueryable("rating")]
	public int Rating { get; set; }

	[UserQueryable("length")]
	public TimeSpan? MediaLength { get; set; }

	[UserQueryable("order")]
	public int? Order { get; set; } = null;

	public bool Locked { get; set; } = false;

	[NotMapped]
	public string LengthString => MediaLength?.ToString(@"hh\:mm\:ss") ?? "?";

	[NotMapped]
	public string TruncatedDescription
	{
		get
		{
			string truncated = Description?.Length > 100 ? Description[..97] + "..." : Description ?? "";
			return WhiteSpace().Replace(truncated, " ");
		}
	}

	[GeneratedRegex("\\s+")]
	private static partial Regex WhiteSpace();

	public int CompareTo(Media? other)
	{
		return Id.CompareTo(other?.Id);
	}

	public override string ToString()
	{
		return $"#{Id} || {Title} || {(Hash != null ? Convert.ToBase64String(Hash) : "no hash")}";
	}

	public string GenerateFileName(string extension)
	{
		StringBuilder sb = new(Title);
		if (Album != null)
		{
			sb.Append(" – ");
			sb.Append(Album);
			if (Order != null)
			{
				sb.Append('[');
				sb.Append(Order);
				sb.Append(']');
			}
		}

		if (PrimaryArtist != null)
		{
			sb.Append(" – ");
			sb.Append(PrimaryArtist);
		}

		sb.Append('.');
		sb.Append(extension);
		return sb.ToString();
	}
}
