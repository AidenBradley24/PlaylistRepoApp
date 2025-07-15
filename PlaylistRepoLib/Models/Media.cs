using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace PlaylistRepoLib.Models;

public partial class Media
{
	[Key] public int Id { get; set; }

	public string? FilePath { get; set; }
	public byte[]? Hash { get; set; }
	public required string Title { get; set; }

	public string[]? Artists { get; set; }
	public string? Album { get; set; }
	public string? Description { get; set; }
	public int Rating { get; set; }
	public TimeSpan? MediaLength { get; set; }

	public const int UNINITIALIZED = -1;

	public MediaSettings Settings { get; set; }

	[NotMapped]
	public bool Locked
	{
		get => Settings.HasFlag(MediaSettings.locked);
		set
		{
			if (value)
			{
				Settings |= MediaSettings.locked;
			}
			else
			{
				Settings &= ~MediaSettings.locked;
			}
		}
	}

	[NotMapped]
	public bool Downloaded
	{
		get => Settings.HasFlag(MediaSettings.downloaded);
		internal set
		{
			if (value)
			{
				Settings |= MediaSettings.downloaded;
			}
			else
			{
				Settings &= ~MediaSettings.downloaded;
			}
		}
	}

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
}

[Flags]
public enum MediaSettings
{
	none = 0,
	removeMe = 1 << 0,
	locked = 1 << 1,
	downloaded = 1 << 2,
}
