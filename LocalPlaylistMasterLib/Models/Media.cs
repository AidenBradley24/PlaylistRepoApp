using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace LocalPlaylistMasterLib.Models;

public partial class Media
{
    [Key] public int Id { get; set; }

    public string? FilePath { get; set; }
    public byte[]? Hash { get; set; }

    public required string Name { get; set; }

    public string? Artists { get; set; }
    public string? Album { get; set; }
    public string? Description { get; set; }
    public int Rating { get; set; }
    public double TimeInSeconds { get; set; }

	public const int UNINITIALIZED = -1;

	public MediaSettings Settings { get; set; }
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

	public string LengthString { get => TimeInSeconds == UNINITIALIZED ? "?" : TimeSpan.FromSeconds(TimeInSeconds).ToString(@"hh\:mm\:ss"); }

	public string TruncatedDescription
	{
		get
		{
			string truncated = Description?.Length > 100 ? Description[..97] + "..." : Description ?? "";
			return WhiteSpace().Replace(truncated, " ");
		}
	}

	public TimeSpan Length { get => TimeSpan.FromSeconds(TimeInSeconds); }

	public string[] GetArtists()
    {
        return Artists?.Split(',') ?? [];
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex WhiteSpace();

    public int CompareTo(Media? other)
    {
        return Id.CompareTo(other?.Id);
    }

    public override string ToString()
    {
        return $"#{Id} -- {Name}";
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
