using PlaylistRepoLib.UserQueries;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace PlaylistRepoLib.Models;

[PrimaryUserQueryable(nameof(Title))]
public partial class Media
{
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	[Key] public int Id { get; set; }

	[ForeignKey(nameof(Source))] public int? RemoteId { get; set; }

	[JsonIgnore]
	public RemotePlaylist? Source { get; set; }

	/// <summary>
	/// A unique identifier for this media on any remote server.
	/// </summary>
	public string? RemoteUID { get; set; }

	[JsonIgnore]
	[NotMapped] public FileInfo? File => FilePath != null ? new(FilePath) : null;

	public string? FilePath { get; set; }
	public byte[]? Hash { get; set; }

	[UserQueryable("title")]
	public string Title { get; set; } = "unnamed media";

	[UserQueryable("artist")]
	[NotMapped]
	[JsonIgnore]
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
		StringBuilder sb = new();
		sb.Append(Id);
		sb.Append('\t');
		sb.Append(Title);

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

		if (Hash != null)
		{
			sb.Append('\t');
			sb.Append('[');
			sb.Append(Convert.ToBase64String(Hash));
			sb.Append(']');
		}

		return sb.ToString();
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
