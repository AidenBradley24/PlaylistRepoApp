# Playlist Repository

Store your media in a system like GIT.

Host, download, query, and manage your media with one system.
Use yt-dlp to download media playlists from various platforms like YouTube. Then combine them into a local database and media server.

Designed to completely replace and combine the functionality of my previous YTAutoMusic and Local Playlist Master.

## Contents:
- A CLI to run in a terminal with similar commands as git
- A media server to dynamically host playlists
- An advanced yet simple method of creating playlist and querying media
- Export playlists into common formats

---

## Getting Started

### OPTION 1 -- *Build Yourself*
- Clone this github repository
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js & npm](https://nodejs.org/en/download)
- [Inno Setup (to make installer, windows only)](https://jrsoftware.org/isdl.php)

##### Commands WINDOWS POWERSHELL (run in the project root):
```powershell
dotnet publish PlaylistRepoAPI/PlaylistRepoAPI.csproj -c Release -r win-x64 -o publish/PlaylistRepoAPI
dotnet publish PlaylistRepoCLI/PlaylistRepoCLI.csproj -c Release -r win-x64 -o publish/PlaylistRepoCLI

# BUILD INSTALLER...
iscc scripts/wininstaller.iss /dMyAppVersion=0.0.0 # <- INSERT VERSION HERE

# ...OR RUN INSTALL SCRIPT
powershell.exe -ExecutionPolicy Bypass -File .\scripts\install.ps1
```

##### Commands LINUX TERMINAL (run in the project root):
```console
dotnet publish PlaylistRepoAPI/PlaylistRepoAPI.csproj-c Release -r linux-x64 -o publish/PlaylistRepoAPI
dotnet publish PlaylistRepoCLI/PlaylistRepoCLI.csproj -c Release -r linux-x64 -o publish/PlaylistRepoCLI
bash scripts/install.bash
```

### OPTION 2 -- *Download installer*
- [Github Releases](https://github.com/AidenBradley24/PlaylistRepoApp/releases)

### *FINALLY*
- [yt-dlp](https://github.com/yt-dlp/yt-dlp) (ensure it's installed and available in your PATH)
- Run the installer.
- Optionally, or if you want you can build the app and use it in place. But you will need to manually add the app to your PATH

---

# CLI Commands

## host
Start the server

**Options:**
- `-d, --dir`: Specify a directory to run the serve on. Defaults to current directory.
- `--open-browser`: Open the browser automatically. Default is true.

**Example:**
```bash
playlistrepo host
```

## init
Initialize a new playlist repository.

**Options:**
- `-d, --dir`: Specify a directory to serve. Defaults to current directory. Ignored if the url is set.
- `-l, --url`: Specify a URL to a running API.

**Example:**
```bash
playlistrepo init
```

## ingest
Add new media files within the repo directory to the database.

**Options:**
- `<files>`: Search criteria for files to add. Default is "*".
- `-d, --dir`: Specify a directory to serve. Defaults to current directory. Ignored if the url is set.
- `-l, --url`: Specify a URL to a running API.

**Example:**
```bash
playlistrepo ingest "*.mp3"
```

## fetch
Fetch media metadata from a remote playlist.

**Options:**
- `<id>`: Numeric ID or the name of the remote playlist.
- `-d, --dir`: Specify a directory to serve. Defaults to current directory. Ignored if the url is set.
- `-l, --url`: Specify a URL to a running API.

**Example:**
```bash
playlistrepo fetch 1
```

## sync
Sync media metadata and download from a remote playlist.

**Options:**
- `<id>`: Numeric ID or the name of the remote playlist.
- `-d, --dir`: Specify a directory to serve. Defaults to current directory. Ignored if the url is set.
- `-l, --url`: Specify a URL to a running API.

**Example:**
```bash
playlistrepo fetch 1
```

## add
Add a new remote playlist to this repo.

**Options:**
- `<type>`: "internet" or "ytdlp".
- `<url>`: URL of the specified remote playlist.
- `-m, --media-type`: MIME type of the media in the playlist.
- `-n, --name`: Name of the remote playlist.
- `-d, --description`: Description of the remote playlist.
- `-d, --dir`: Specify a directory to serve. Defaults to current directory. Ignored if the url is set.
- `-l, --url`: Specify a URL to a running API.

**Example:**
```bash
playlistrepo add internet https://example.com/playlist.xspf
```

## create
Create a new playlist from media on this repo.

**Options:**
- `<contents>`: User Query to specify the contents of this playlist.
- `<title>`: Title of the playlist.
- `<description>`: Description of the playlist.
- `-d, --dir`: Specify a directory to serve. Defaults to current directory. Ignored if the url is set.
- `-l, --url`: Specify a URL to a running API.

**Example:**
```bash
playlistrepo create "genre:rock" "My Rock Playlist" "A playlist of my favorite rock songs"
```

## list
List contents of the database.

**Options:**
- `<User query>`: A filter upon the output returned.
- `-m, --media`: List media.
- `-r, --remote-playlists`: List remote playlists.
- `-p, --playlists`: List playlists.
- `--pagesize`: Number of entries to display per page. Default is 50.
- `--pagenum`: Page number to display. Default is 1.
- `-d, --dir`: Specify a directory to serve. Defaults to current directory. Ignored if the url is set.
- `-l, --url`: Specify a URL to a running API.

**Example:**
```bash
playlistrepo list --media --playlists
```

## export
Export a particular playlist in a specific format: .xspf .m3u8 .zip .csv

**Options:**
- `<Playlist ID>`: ID of the playlist to export.
- `<Export Path>`: Full path of exported file.
- `-d, --dir`: Specify a directory to serve. Defaults to current directory. Ignored if the url is set.
- `-l, --url`: Specify a URL to a running API.

**Example:**
```bash
playlistrepo export 1 "C:\Exports\playlist.zip"
```
