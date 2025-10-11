#!/bin/bash

# Set the source and destination directories
SRC_DIR="publish/PlaylistRepoCLI"
DEST_DIR="/opt/PlaylistRepo"

# Create the destination directory if it doesn't exist
sudo mkdir -p "$DEST_DIR"

# Move the published content to the destination directory
sudo mv "$SRC_DIR"/* "$DEST_DIR"

# Add the destination directory to the system PATH
sudo tee -a /etc/profile.d/playlistrepocli.sh << EOF
export PATH="\$PATH:$DEST_DIR"
EOF

echo "PlaylistRepo has been installed to $DEST_DIR and added to the system PATH."