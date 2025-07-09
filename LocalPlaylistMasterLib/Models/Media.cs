using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace LocalPlaylistMasterLib.Models;

public partial class Media
{
    [Key] public int Id { get; set; }

    public required string FilePath { get; set; }

    public required string Name { get; set; }
    public int Remote { get; set; }
    public string? RemoteId { get; set; }
    public string? Artists { get; set; }
    public string? Album { get; set; }
    public string? Description { get; set; }
    public int Rating { get; set; }
    public double TimeInSeconds { get; set; }

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

    public string[] GetArtists()
    {
        return Artists?.Split(',') ?? [];
    }

    public const int UNINITIALIZED = -1;

    public Media(int id, string name, int remote, string remoteId, string artists, string album, string description,
        int rating, double timeInSeconds, MediaSettings settings, string miscJson)
    {
        Id = id;
        Name = name;
        Remote = remote;
        RemoteId = remoteId;
        Artists = artists;
        Album = album;
        Description = description;
        Rating = rating;
        TimeInSeconds = timeInSeconds;
        Settings = settings;
    }

    public Media()
    {
        Id = UNINITIALIZED;
        Name = "";
        Remote = UNINITIALIZED;
        RemoteId = "";
        Artists = "";
        Album = "";
        Description = "";
        Rating = UNINITIALIZED;
        TimeInSeconds = UNINITIALIZED;
    }

    public Media(Media old)
    {
        Id = old.Id;
        Name = old.Name;
        Remote = old.Remote;
        RemoteId = old.RemoteId;
        Artists = old.Artists;
        Album = old.Album;
        Description = old.Description;
        Rating = old.Rating;
        TimeInSeconds = old.TimeInSeconds;
        Settings = old.Settings;
    }

    public string LengthString { get => TimeInSeconds == UNINITIALIZED ? "?" : TimeSpan.FromSeconds(TimeInSeconds).ToString(@"hh\:mm\:ss"); }

    public string TruncatedDescription 
    { 
        get
        {
            string truncated = Description.Length > 100 ? Description[..97] + "..." : Description;
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
        return $"#{Id} -- {Name}";
    }

    public TimeSpan Length { get => TimeSpan.FromSeconds(TimeInSeconds); }
}

[Flags]
public enum MediaSettings
{
    none = 0,
    removeMe = 1 << 0,
    locked = 1 << 1,
    downloaded = 1 << 2,
}
