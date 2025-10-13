# Set the source and destination directories
$SrcDir = "publish\PlaylistRepoCLI"
$DestDir = "C:\Program Files\PlaylistRepo"

# Create the destination directory if it doesn't exist
New-Item -ItemType Directory -Force -Path $DestDir | Out-Null

# Move the published content to the destination directory
Move-Item -Path "$SrcDir\*" -Destination $DestDir -Force

# Add batch that points to cli
Copy-Item -Path "scripts\playlistrepo.bat" -Destination $SrcDir

# Add the destination directory to the system PATH
$PathVariable = [System.Environment]::GetEnvironmentVariable('Path', 'Machine')
if (-not $PathVariable.Contains($DestDir)) {
    [System.Environment]::SetEnvironmentVariable('Path', "$PathVariable;$DestDir", 'Machine')
    Write-Host "PlaylistRepo has been added to the system PATH."
}
else {
    Write-Host "PlaylistRepo is already in the system PATH."
}

Write-Host "PlaylistRepo has been installed to $DestDir."